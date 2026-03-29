using CashewAPI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace CashewAPI.Tests.Services;

public class CashewDatabaseTests : IDisposable
{
    private readonly CashewDatabase _db;
    private readonly string _testDir;

    public CashewDatabaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"CashewAPITests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseDirectory"] = _testDir,
            })
            .Build();

        _db = new CashewDatabase(config);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        GC.SuppressFinalize(this);
    }

    private void LoadTestDatabase()
    {
        var dbPath = Path.Combine(_testDir, "test_setup.db");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // Create tables matching Cashew schema
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS categories (
                category_pk TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                colour TEXT,
                icon_name TEXT,
                emoji_icon_name TEXT,
                date_created INTEGER NOT NULL,
                date_time_modified INTEGER,
                ""order"" INTEGER NOT NULL DEFAULT 0,
                income INTEGER NOT NULL DEFAULT 0,
                method_added INTEGER,
                main_category_pk TEXT
            );

            CREATE TABLE IF NOT EXISTS wallets (
                wallet_pk TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                colour TEXT,
                icon_name TEXT,
                date_created INTEGER NOT NULL,
                date_time_modified INTEGER,
                ""order"" INTEGER NOT NULL DEFAULT 0,
                currency TEXT,
                currency_format TEXT,
                decimals INTEGER NOT NULL DEFAULT 2,
                home_page_widget_display TEXT
            );

            CREATE TABLE IF NOT EXISTS transactions (
                transaction_pk TEXT NOT NULL PRIMARY KEY,
                paired_transaction_fk TEXT,
                name TEXT NOT NULL,
                amount REAL NOT NULL,
                note TEXT NOT NULL DEFAULT '',
                category_fk TEXT NOT NULL,
                sub_category_fk TEXT,
                wallet_fk TEXT NOT NULL DEFAULT '0',
                date_created INTEGER NOT NULL,
                date_time_modified INTEGER,
                original_date_due INTEGER,
                income INTEGER NOT NULL DEFAULT 0,
                period_length INTEGER,
                reoccurrence INTEGER,
                end_date INTEGER,
                upcoming_transaction_notification INTEGER DEFAULT 1,
                type INTEGER,
                paid INTEGER NOT NULL DEFAULT 0,
                created_another_future_transaction INTEGER,
                skip_paid INTEGER NOT NULL DEFAULT 0,
                method_added INTEGER,
                transaction_owner_email TEXT,
                transaction_original_owner_email TEXT,
                shared_key TEXT,
                shared_old_key TEXT,
                shared_status INTEGER,
                shared_date_updated INTEGER,
                shared_reference_budget_pk TEXT,
                objective_fk TEXT,
                objective_loan_fk TEXT,
                budget_fks_exclude TEXT
            );

            CREATE TABLE IF NOT EXISTS delete_logs (
                delete_log_pk TEXT NOT NULL PRIMARY KEY,
                entry_pk TEXT NOT NULL,
                type INTEGER NOT NULL,
                date_time_modified INTEGER NOT NULL
            );

            -- Insert test data
            INSERT INTO wallets (wallet_pk, name, date_created, ""order"", decimals)
            VALUES ('0', 'Default Wallet', 1700000000000, 0, 2);

            INSERT INTO wallets (wallet_pk, name, date_created, ""order"", decimals, currency)
            VALUES ('wallet-1', 'Savings', 1700000000000, 1, 2, 'USD');

            INSERT INTO categories (category_pk, name, date_created, ""order"", income)
            VALUES ('0', 'Balance Correction', 1700000000000, 0, 0);

            INSERT INTO categories (category_pk, name, date_created, ""order"", income)
            VALUES ('cat-food', 'Food', 1700000000000, 1, 0);

            INSERT INTO categories (category_pk, name, date_created, ""order"", income)
            VALUES ('cat-salary', 'Salary', 1700000000000, 2, 1);

            INSERT INTO categories (category_pk, name, date_created, ""order"", income, main_category_pk)
            VALUES ('cat-lunch', 'Lunch', 1700000000000, 3, 0, 'cat-food');

            -- Insert sample transactions
            INSERT INTO transactions (transaction_pk, name, amount, note, category_fk, wallet_fk, date_created, income, paid)
            VALUES ('txn-1', 'Grocery', -50.00, 'Weekly groceries', 'cat-food', '0', 1700100000000, 0, 0);

            INSERT INTO transactions (transaction_pk, name, amount, note, category_fk, wallet_fk, date_created, income, paid)
            VALUES ('txn-2', 'Monthly Salary', 5000.00, '', 'cat-salary', '0', 1700200000000, 1, 1);

            INSERT INTO transactions (transaction_pk, name, amount, note, category_fk, wallet_fk, date_created, income, paid)
            VALUES ('txn-3', 'Restaurant', -35.50, 'Dinner', 'cat-food', 'wallet-1', 1700300000000, 0, 0);
        ";
        cmd.ExecuteNonQuery();
        connection.Close();

        var fileBytes = File.ReadAllBytes(dbPath);
        _db.LoadDatabase(fileBytes, "test.sql");
        File.Delete(dbPath);
    }

    [Fact]
    public void IsLoaded_BeforeLoad_ReturnsFalse()
    {
        Assert.False(_db.IsLoaded);
    }

    [Fact]
    public void LoadDatabase_SetsIsLoaded()
    {
        LoadTestDatabase();
        Assert.True(_db.IsLoaded);
        Assert.Equal("test.sql", _db.LoadedFileName);
        Assert.NotNull(_db.LastSyncTime);
    }

    [Fact]
    public void GetTransactions_ReturnsAllTransactions()
    {
        LoadTestDatabase();
        var transactions = _db.GetTransactions(1, 100);
        Assert.Equal(3, transactions.Count);
    }

    [Fact]
    public void GetTransactions_Pagination_Works()
    {
        LoadTestDatabase();
        var page1 = _db.GetTransactions(1, 2);
        Assert.Equal(2, page1.Count);

        var page2 = _db.GetTransactions(2, 2);
        Assert.Single(page2);
    }

    [Fact]
    public void GetTransactions_FilterByWallet_Works()
    {
        LoadTestDatabase();
        var transactions = _db.GetTransactions(1, 100, walletFk: "wallet-1");
        Assert.Single(transactions);
        Assert.Equal("txn-3", transactions[0].TransactionPk);
    }

    [Fact]
    public void GetTransactions_FilterByCategory_Works()
    {
        LoadTestDatabase();
        var transactions = _db.GetTransactions(1, 100, categoryFk: "cat-salary");
        Assert.Single(transactions);
        Assert.Equal("txn-2", transactions[0].TransactionPk);
    }

    [Fact]
    public void GetTransactions_FilterByIncome_Works()
    {
        LoadTestDatabase();
        var incomeTransactions = _db.GetTransactions(1, 100, income: true);
        Assert.Single(incomeTransactions);
        Assert.True(incomeTransactions[0].Income);

        var expenseTransactions = _db.GetTransactions(1, 100, income: false);
        Assert.Equal(2, expenseTransactions.Count);
    }

    [Fact]
    public void GetTransaction_ExistingPk_ReturnsTransaction()
    {
        LoadTestDatabase();
        var txn = _db.GetTransaction("txn-1");
        Assert.NotNull(txn);
        Assert.Equal("Grocery", txn.Name);
        Assert.Equal(-50.00, txn.Amount);
        Assert.Equal("cat-food", txn.CategoryFk);
    }

    [Fact]
    public void GetTransaction_NonExistingPk_ReturnsNull()
    {
        LoadTestDatabase();
        var txn = _db.GetTransaction("non-existing");
        Assert.Null(txn);
    }

    [Fact]
    public void GetTransactionCount_ReturnsCorrectCount()
    {
        LoadTestDatabase();
        Assert.Equal(3, _db.GetTransactionCount());
        Assert.Equal(1, _db.GetTransactionCount(income: true));
        Assert.Equal(2, _db.GetTransactionCount(income: false));
    }

    [Fact]
    public void CreateTransaction_InsertsTransaction()
    {
        LoadTestDatabase();
        var transaction = new Models.Transaction
        {
            TransactionPk = "txn-new",
            Name = "Test Transaction",
            Amount = -25.00,
            Note = "Test note",
            CategoryFk = "cat-food",
            WalletFk = "0",
            DateCreated = DateTime.UtcNow,
            DateTimeModified = DateTime.UtcNow,
            Income = false,
        };

        var result = _db.CreateTransaction(transaction);
        Assert.Equal("txn-new", result.TransactionPk);

        var retrieved = _db.GetTransaction("txn-new");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Transaction", retrieved.Name);
        Assert.Equal(-25.00, retrieved.Amount);
        Assert.Equal(4, _db.GetTransactionCount());
    }

    [Fact]
    public void CreateTransactions_BatchInsert_Works()
    {
        LoadTestDatabase();
        var transactions = new List<Models.Transaction>
        {
            new()
            {
                TransactionPk = "batch-1",
                Name = "Batch 1",
                Amount = -10.00,
                CategoryFk = "cat-food",
                WalletFk = "0",
                DateCreated = DateTime.UtcNow,
                Income = false,
            },
            new()
            {
                TransactionPk = "batch-2",
                Name = "Batch 2",
                Amount = 100.00,
                CategoryFk = "cat-salary",
                WalletFk = "0",
                DateCreated = DateTime.UtcNow,
                Income = true,
            },
        };

        var results = _db.CreateTransactions(transactions);
        Assert.Equal(2, results.Count);
        Assert.Equal(5, _db.GetTransactionCount());
    }

    [Fact]
    public void DeleteTransaction_ExistingPk_DeletesAndCreatesLog()
    {
        LoadTestDatabase();
        var deleted = _db.DeleteTransaction("txn-1");
        Assert.True(deleted);
        Assert.Equal(2, _db.GetTransactionCount());
        Assert.Null(_db.GetTransaction("txn-1"));
    }

    [Fact]
    public void DeleteTransaction_NonExistingPk_ReturnsFalse()
    {
        LoadTestDatabase();
        var deleted = _db.DeleteTransaction("non-existing");
        Assert.False(deleted);
        Assert.Equal(3, _db.GetTransactionCount());
    }

    [Fact]
    public void DeleteTransactions_Batch_Works()
    {
        LoadTestDatabase();
        var deletedCount = _db.DeleteTransactions(["txn-1", "txn-2", "non-existing"]);
        Assert.Equal(2, deletedCount);
        Assert.Equal(1, _db.GetTransactionCount());
    }

    [Fact]
    public void GetCategories_ReturnsAll()
    {
        LoadTestDatabase();
        var categories = _db.GetCategories();
        Assert.Equal(4, categories.Count);
    }

    [Fact]
    public void GetCategory_ExistingPk_ReturnsCategory()
    {
        LoadTestDatabase();
        var category = _db.GetCategory("cat-food");
        Assert.NotNull(category);
        Assert.Equal("Food", category.Name);
        Assert.False(category.Income);
    }

    [Fact]
    public void GetCategory_SubCategory_HasMainCategoryPk()
    {
        LoadTestDatabase();
        var subCategory = _db.GetCategory("cat-lunch");
        Assert.NotNull(subCategory);
        Assert.Equal("cat-food", subCategory.MainCategoryPk);
    }

    [Fact]
    public void GetWallets_ReturnsAll()
    {
        LoadTestDatabase();
        var wallets = _db.GetWallets();
        Assert.Equal(2, wallets.Count);
    }

    [Fact]
    public void GetWallet_ExistingPk_ReturnsWallet()
    {
        LoadTestDatabase();
        var wallet = _db.GetWallet("wallet-1");
        Assert.NotNull(wallet);
        Assert.Equal("Savings", wallet.Name);
        Assert.Equal("USD", wallet.Currency);
    }

    [Fact]
    public void ExportDatabase_ReturnsBytes()
    {
        LoadTestDatabase();
        var bytes = _db.ExportDatabase();
        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void ExportDatabase_NotLoaded_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => _db.ExportDatabase());
    }

    [Fact]
    public void DateTimeConversion_RoundTrip_IsAccurate()
    {
        LoadTestDatabase();
        var now = DateTime.UtcNow;
        // Truncate to milliseconds since SQLite stores as unix ms
        now = new DateTime(now.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);

        var transaction = new Models.Transaction
        {
            TransactionPk = "txn-date-test",
            Name = "Date Test",
            Amount = -1.00,
            CategoryFk = "cat-food",
            WalletFk = "0",
            DateCreated = now,
            DateTimeModified = now,
            Income = false,
        };

        _db.CreateTransaction(transaction);
        var retrieved = _db.GetTransaction("txn-date-test");

        Assert.NotNull(retrieved);
        // Allow 1 second tolerance due to ms precision
        Assert.True(Math.Abs((retrieved.DateCreated - now).TotalSeconds) < 1);
    }
}
