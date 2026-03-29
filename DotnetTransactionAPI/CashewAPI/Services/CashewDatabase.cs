// CashewDatabase.cs — SQLite-backed implementation of ICashewDatabase.
// Manages a local copy of the Cashew budget app's SQLite database file.
// All date/time values are stored as Unix milliseconds (Int64) in the database.

using CashewAPI.Models;
using Microsoft.Data.Sqlite;

namespace CashewAPI.Services;

/// <summary>
/// Singleton service that loads a Cashew SQLite database file into a local directory
/// and provides thread-safe CRUD operations against it.
/// Write operations are serialised with a <see cref="SemaphoreSlim"/> to prevent concurrent modification.
/// </summary>
public class CashewDatabase : ICashewDatabase, IDisposable
{
    private readonly string _dbDirectory;
    private string? _dbPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc />
    public bool IsLoaded => _dbPath != null && File.Exists(_dbPath);

    /// <inheritdoc />
    public string? LoadedFileName { get; private set; }

    /// <inheritdoc />
    public DateTime? LastSyncTime { get; private set; }

    /// <summary>
    /// Initialises the database directory and auto-detects any previously loaded database file.
    /// </summary>
    public CashewDatabase(IConfiguration configuration)
    {
        _dbDirectory = configuration.GetValue<string>("DatabaseDirectory")
            ?? Path.Combine(Path.GetTempPath(), "CashewAPI");
        Directory.CreateDirectory(_dbDirectory);

        // Auto-detect existing database file from a previous session
        var existingDb = Path.Combine(_dbDirectory, "cashew.db");
        if (File.Exists(existingDb))
        {
            _dbPath = existingDb;
            LoadedFileName = "cashew.db";
            LastSyncTime = File.GetLastWriteTimeUtc(existingDb);
        }
    }

    /// <summary>
    /// Builds a SQLite connection string pointing to the currently loaded database file.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no database has been loaded.</exception>
    private string GetConnectionString()
    {
        if (_dbPath == null)
            throw new InvalidOperationException("Database not loaded. Call sync/pull first.");
        return $"Data Source={_dbPath}";
    }

    /// <inheritdoc />
    public void LoadDatabase(byte[] fileBytes, string fileName)
    {
        _lock.Wait();
        try
        {
            _dbPath = Path.Combine(_dbDirectory, "cashew.db");
            File.WriteAllBytes(_dbPath, fileBytes);
            LoadedFileName = fileName;
            LastSyncTime = DateTime.UtcNow;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public byte[] ExportDatabase()
    {
        if (_dbPath == null || !File.Exists(_dbPath))
            throw new InvalidOperationException("Database not loaded.");
        return File.ReadAllBytes(_dbPath);
    }

    // --- DateTime Helpers ---
    // Cashew stores dates as integer milliseconds since Unix epoch

    /// <summary>Converts a <see cref="DateTime"/> to Unix epoch milliseconds.</summary>
    private static long DateTimeToUnixMs(DateTime dt)
        => new DateTimeOffset(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime())
            .ToUnixTimeMilliseconds();

    /// <summary>Converts Unix epoch milliseconds to a UTC <see cref="DateTime"/>.</summary>
    private static DateTime UnixMsToDateTime(long ms)
        => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

    /// <summary>Reads a nullable DateTime column stored as Unix milliseconds.</summary>
    private static DateTime? ReadNullableDateTime(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        return UnixMsToDateTime(reader.GetInt64(ordinal));
    }

    /// <summary>Reads a nullable Int32 column.</summary>
    private static int? ReadNullableInt(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    /// <summary>Reads a nullable String column.</summary>
    private static string? ReadNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    /// <summary>Reads a boolean column stored as an integer (0/1).</summary>
    private static bool ReadBool(SqliteDataReader reader, int ordinal)
        => !reader.IsDBNull(ordinal) && reader.GetInt32(ordinal) == 1;

    /// <summary>Reads a nullable boolean column stored as an integer (0/1).</summary>
    private static bool? ReadNullableBool(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal) == 1;

    // --- Transactions ---

    /// <inheritdoc />
    public List<Transaction> GetTransactions(int page, int pageSize,
        string? walletFk = null, string? categoryFk = null,
        DateTime? startDate = null, DateTime? endDate = null,
        bool? income = null)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        var (whereClause, parameters) = BuildTransactionFilter(walletFk, categoryFk, startDate, endDate, income);

        var sql = $"SELECT * FROM transactions {whereClause} ORDER BY date_created DESC LIMIT @limit OFFSET @offset";
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in parameters) cmd.Parameters.Add(p);
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        var results = new List<Transaction>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(ReadTransaction(reader));
        }
        return results;
    }

    /// <inheritdoc />
    public int GetTransactionCount(string? walletFk = null, string? categoryFk = null,
        DateTime? startDate = null, DateTime? endDate = null,
        bool? income = null)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        var (whereClause, parameters) = BuildTransactionFilter(walletFk, categoryFk, startDate, endDate, income);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM transactions {whereClause}";
        foreach (var p in parameters) cmd.Parameters.Add(p);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <inheritdoc />
    public Transaction? GetTransaction(string transactionPk)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM transactions WHERE transaction_pk = @pk";
        cmd.Parameters.AddWithValue("@pk", transactionPk);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadTransaction(reader) : null;
    }

    /// <inheritdoc />
    public Transaction CreateTransaction(Transaction transaction)
    {
        _lock.Wait();
        try
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            InsertTransaction(connection, transaction);
            return transaction;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public List<Transaction> CreateTransactions(List<Transaction> transactions)
    {
        _lock.Wait();
        try
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            using var sqliteTransaction = connection.BeginTransaction();

            foreach (var t in transactions)
            {
                InsertTransaction(connection, t);
            }

            sqliteTransaction.Commit();
            return transactions;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public bool DeleteTransaction(string transactionPk)
    {
        _lock.Wait();
        try
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            using var sqliteTransaction = connection.BeginTransaction();

            // Check if transaction exists
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM transactions WHERE transaction_pk = @pk";
            checkCmd.Parameters.AddWithValue("@pk", transactionPk);
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            if (!exists) return false;

            // Create delete log entry
            InsertDeleteLog(connection, transactionPk, DeleteLogType.Transaction);

            // Delete the transaction
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM transactions WHERE transaction_pk = @pk";
            deleteCmd.Parameters.AddWithValue("@pk", transactionPk);
            deleteCmd.ExecuteNonQuery();

            sqliteTransaction.Commit();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public int DeleteTransactions(List<string> transactionPks)
    {
        _lock.Wait();
        try
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            using var sqliteTransaction = connection.BeginTransaction();

            int deleted = 0;
            foreach (var pk in transactionPks)
            {
                // Check existence before deleting to track accurate count
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM transactions WHERE transaction_pk = @pk";
                checkCmd.Parameters.AddWithValue("@pk", pk);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (!exists) continue;

                InsertDeleteLog(connection, pk, DeleteLogType.Transaction);

                using var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM transactions WHERE transaction_pk = @pk";
                deleteCmd.Parameters.AddWithValue("@pk", pk);
                deleteCmd.ExecuteNonQuery();
                deleted++;
            }

            sqliteTransaction.Commit();
            return deleted;
        }
        finally
        {
            _lock.Release();
        }
    }

    // --- Categories ---

    /// <inheritdoc />
    public List<Category> GetCategories()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM categories ORDER BY \"order\" ASC";
        var results = new List<Category>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(ReadCategory(reader));
        }
        return results;
    }

    /// <inheritdoc />
    public Category? GetCategory(string categoryPk)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM categories WHERE category_pk = @pk";
        cmd.Parameters.AddWithValue("@pk", categoryPk);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadCategory(reader) : null;
    }

    // --- Wallets ---

    /// <inheritdoc />
    public List<Wallet> GetWallets()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM wallets ORDER BY \"order\" ASC";
        var results = new List<Wallet>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(ReadWallet(reader));
        }
        return results;
    }

    /// <inheritdoc />
    public Wallet? GetWallet(string walletPk)
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM wallets WHERE wallet_pk = @pk";
        cmd.Parameters.AddWithValue("@pk", walletPk);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadWallet(reader) : null;
    }

    // --- Private Helpers ---

    /// <summary>
    /// Builds a SQL WHERE clause and parameter list from the supplied transaction filter criteria.
    /// Dates are converted to Unix milliseconds to match the database storage format.
    /// </summary>
    private static (string whereClause, List<SqliteParameter> parameters) BuildTransactionFilter(
        string? walletFk, string? categoryFk,
        DateTime? startDate, DateTime? endDate,
        bool? income)
    {
        var conditions = new List<string>();
        var parameters = new List<SqliteParameter>();

        if (walletFk != null)
        {
            conditions.Add("wallet_fk = @walletFk");
            parameters.Add(new SqliteParameter("@walletFk", walletFk));
        }
        if (categoryFk != null)
        {
            conditions.Add("category_fk = @categoryFk");
            parameters.Add(new SqliteParameter("@categoryFk", categoryFk));
        }
        if (startDate != null)
        {
            conditions.Add("date_created >= @startDate");
            parameters.Add(new SqliteParameter("@startDate", DateTimeToUnixMs(startDate.Value)));
        }
        if (endDate != null)
        {
            conditions.Add("date_created <= @endDate");
            parameters.Add(new SqliteParameter("@endDate", DateTimeToUnixMs(endDate.Value)));
        }
        if (income != null)
        {
            conditions.Add("income = @income");
            parameters.Add(new SqliteParameter("@income", income.Value ? 1 : 0));
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        return (whereClause, parameters);
    }

    /// <summary>
    /// Inserts a single transaction row into the database.
    /// Converts all DateTime and bool properties to their SQLite-compatible representations.
    /// </summary>
    private static void InsertTransaction(SqliteConnection connection, Transaction t)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO transactions (
                transaction_pk, paired_transaction_fk, name, amount, note,
                category_fk, sub_category_fk, wallet_fk, date_created, date_time_modified,
                original_date_due, income, period_length, reoccurrence, end_date,
                upcoming_transaction_notification, type, paid, created_another_future_transaction,
                skip_paid, method_added, transaction_owner_email, transaction_original_owner_email,
                shared_key, shared_old_key, shared_status, shared_date_updated,
                shared_reference_budget_pk, objective_fk, objective_loan_fk, budget_fks_exclude
            ) VALUES (
                @pk, @pairedFk, @name, @amount, @note,
                @categoryFk, @subCategoryFk, @walletFk, @dateCreated, @dateTimeModified,
                @originalDateDue, @income, @periodLength, @reoccurrence, @endDate,
                @upcomingNotification, @type, @paid, @createdFuture,
                @skipPaid, @methodAdded, @ownerEmail, @originalOwnerEmail,
                @sharedKey, @sharedOldKey, @sharedStatus, @sharedDateUpdated,
                @sharedRefBudgetPk, @objectiveFk, @objectiveLoanFk, @budgetFksExclude
            )";

        cmd.Parameters.AddWithValue("@pk", t.TransactionPk);
        cmd.Parameters.AddWithValue("@pairedFk", (object?)t.PairedTransactionFk ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@name", t.Name);
        cmd.Parameters.AddWithValue("@amount", t.Amount);
        cmd.Parameters.AddWithValue("@note", t.Note);
        cmd.Parameters.AddWithValue("@categoryFk", t.CategoryFk);
        cmd.Parameters.AddWithValue("@subCategoryFk", (object?)t.SubCategoryFk ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@walletFk", t.WalletFk);
        cmd.Parameters.AddWithValue("@dateCreated", DateTimeToUnixMs(t.DateCreated));
        cmd.Parameters.AddWithValue("@dateTimeModified", t.DateTimeModified.HasValue ? DateTimeToUnixMs(t.DateTimeModified.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("@originalDateDue", t.OriginalDateDue.HasValue ? DateTimeToUnixMs(t.OriginalDateDue.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("@income", t.Income ? 1 : 0);
        cmd.Parameters.AddWithValue("@periodLength", (object?)t.PeriodLength ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@reoccurrence", (object?)t.Reoccurrence ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@endDate", t.EndDate.HasValue ? DateTimeToUnixMs(t.EndDate.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("@upcomingNotification", t.UpcomingTransactionNotification.HasValue ? (t.UpcomingTransactionNotification.Value ? 1 : 0) : DBNull.Value);
        cmd.Parameters.AddWithValue("@type", (object?)t.Type ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@paid", t.Paid ? 1 : 0);
        cmd.Parameters.AddWithValue("@createdFuture", t.CreatedAnotherFutureTransaction.HasValue ? (t.CreatedAnotherFutureTransaction.Value ? 1 : 0) : DBNull.Value);
        cmd.Parameters.AddWithValue("@skipPaid", t.SkipPaid ? 1 : 0);
        cmd.Parameters.AddWithValue("@methodAdded", (object?)t.MethodAdded ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ownerEmail", (object?)t.TransactionOwnerEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@originalOwnerEmail", (object?)t.TransactionOriginalOwnerEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sharedKey", (object?)t.SharedKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sharedOldKey", (object?)t.SharedOldKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sharedStatus", (object?)t.SharedStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sharedDateUpdated", t.SharedDateUpdated.HasValue ? DateTimeToUnixMs(t.SharedDateUpdated.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("@sharedRefBudgetPk", (object?)t.SharedReferenceBudgetPk ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@objectiveFk", (object?)t.ObjectiveFk ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@objectiveLoanFk", (object?)t.ObjectiveLoanFk ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@budgetFksExclude", (object?)t.BudgetFksExclude ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Records a deletion in the delete_logs table so sync clients can replicate the removal.
    /// </summary>
    /// <param name="connection">Open SQLite connection (within an active transaction).</param>
    /// <param name="entryPk">Primary key of the deleted entity.</param>
    /// <param name="type">Entity type constant from <see cref="DeleteLogType"/>.</param>
    private static void InsertDeleteLog(SqliteConnection connection, string entryPk, int type)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO delete_logs (delete_log_pk, entry_pk, type, date_time_modified)
            VALUES (@pk, @entryPk, @type, @dateTimeModified)";
        cmd.Parameters.AddWithValue("@pk", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@entryPk", entryPk);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.Parameters.AddWithValue("@dateTimeModified", DateTimeToUnixMs(DateTime.UtcNow));
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Maps a SQLite data reader row to a <see cref="Transaction"/> domain object.
    /// </summary>
    private static Transaction ReadTransaction(SqliteDataReader reader)
    {
        return new Transaction
        {
            TransactionPk = reader.GetString(reader.GetOrdinal("transaction_pk")),
            PairedTransactionFk = ReadNullableString(reader, reader.GetOrdinal("paired_transaction_fk")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Amount = reader.GetDouble(reader.GetOrdinal("amount")),
            Note = reader.GetString(reader.GetOrdinal("note")),
            CategoryFk = reader.GetString(reader.GetOrdinal("category_fk")),
            SubCategoryFk = ReadNullableString(reader, reader.GetOrdinal("sub_category_fk")),
            WalletFk = reader.GetString(reader.GetOrdinal("wallet_fk")),
            DateCreated = UnixMsToDateTime(reader.GetInt64(reader.GetOrdinal("date_created"))),
            DateTimeModified = ReadNullableDateTime(reader, reader.GetOrdinal("date_time_modified")),
            OriginalDateDue = ReadNullableDateTime(reader, reader.GetOrdinal("original_date_due")),
            Income = ReadBool(reader, reader.GetOrdinal("income")),
            PeriodLength = ReadNullableInt(reader, reader.GetOrdinal("period_length")),
            Reoccurrence = ReadNullableInt(reader, reader.GetOrdinal("reoccurrence")),
            EndDate = ReadNullableDateTime(reader, reader.GetOrdinal("end_date")),
            UpcomingTransactionNotification = ReadNullableBool(reader, reader.GetOrdinal("upcoming_transaction_notification")),
            Type = ReadNullableInt(reader, reader.GetOrdinal("type")),
            Paid = ReadBool(reader, reader.GetOrdinal("paid")),
            CreatedAnotherFutureTransaction = ReadNullableBool(reader, reader.GetOrdinal("created_another_future_transaction")),
            SkipPaid = ReadBool(reader, reader.GetOrdinal("skip_paid")),
            MethodAdded = ReadNullableInt(reader, reader.GetOrdinal("method_added")),
            TransactionOwnerEmail = ReadNullableString(reader, reader.GetOrdinal("transaction_owner_email")),
            TransactionOriginalOwnerEmail = ReadNullableString(reader, reader.GetOrdinal("transaction_original_owner_email")),
            SharedKey = ReadNullableString(reader, reader.GetOrdinal("shared_key")),
            SharedOldKey = ReadNullableString(reader, reader.GetOrdinal("shared_old_key")),
            SharedStatus = ReadNullableInt(reader, reader.GetOrdinal("shared_status")),
            SharedDateUpdated = ReadNullableDateTime(reader, reader.GetOrdinal("shared_date_updated")),
            SharedReferenceBudgetPk = ReadNullableString(reader, reader.GetOrdinal("shared_reference_budget_pk")),
            ObjectiveFk = ReadNullableString(reader, reader.GetOrdinal("objective_fk")),
            ObjectiveLoanFk = ReadNullableString(reader, reader.GetOrdinal("objective_loan_fk")),
            BudgetFksExclude = ReadNullableString(reader, reader.GetOrdinal("budget_fks_exclude")),
        };
    }

    /// <summary>
    /// Maps a SQLite data reader row to a <see cref="Category"/> domain object.
    /// </summary>
    private static Category ReadCategory(SqliteDataReader reader)
    {
        return new Category
        {
            CategoryPk = reader.GetString(reader.GetOrdinal("category_pk")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Colour = ReadNullableString(reader, reader.GetOrdinal("colour")),
            IconName = ReadNullableString(reader, reader.GetOrdinal("icon_name")),
            EmojiIconName = ReadNullableString(reader, reader.GetOrdinal("emoji_icon_name")),
            DateCreated = UnixMsToDateTime(reader.GetInt64(reader.GetOrdinal("date_created"))),
            DateTimeModified = ReadNullableDateTime(reader, reader.GetOrdinal("date_time_modified")),
            Order = reader.GetInt32(reader.GetOrdinal("order")),
            Income = ReadBool(reader, reader.GetOrdinal("income")),
            MethodAdded = ReadNullableInt(reader, reader.GetOrdinal("method_added")),
            MainCategoryPk = ReadNullableString(reader, reader.GetOrdinal("main_category_pk")),
        };
    }

    /// <summary>
    /// Maps a SQLite data reader row to a <see cref="Wallet"/> domain object.
    /// </summary>
    private static Wallet ReadWallet(SqliteDataReader reader)
    {
        return new Wallet
        {
            WalletPk = reader.GetString(reader.GetOrdinal("wallet_pk")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Colour = ReadNullableString(reader, reader.GetOrdinal("colour")),
            IconName = ReadNullableString(reader, reader.GetOrdinal("icon_name")),
            DateCreated = UnixMsToDateTime(reader.GetInt64(reader.GetOrdinal("date_created"))),
            DateTimeModified = ReadNullableDateTime(reader, reader.GetOrdinal("date_time_modified")),
            Order = reader.GetInt32(reader.GetOrdinal("order")),
            Currency = ReadNullableString(reader, reader.GetOrdinal("currency")),
            CurrencyFormat = ReadNullableString(reader, reader.GetOrdinal("currency_format")),
            Decimals = reader.GetInt32(reader.GetOrdinal("decimals")),
            HomePageWidgetDisplay = ReadNullableString(reader, reader.GetOrdinal("home_page_widget_display")),
        };
    }

    /// <summary>
    /// Releases the write-serialisation semaphore.
    /// </summary>
    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
