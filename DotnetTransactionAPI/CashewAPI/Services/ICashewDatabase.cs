// ICashewDatabase.cs — Interface defining all database operations for the Cashew budget app.
// Implemented by CashewDatabase using a local SQLite file.

using CashewAPI.Models;

namespace CashewAPI.Services;

/// <summary>
/// Abstraction over the Cashew SQLite database.
/// Provides CRUD operations for transactions, and read-only access to categories and wallets.
/// Registered as a singleton so the loaded database persists across requests.
/// </summary>
public interface ICashewDatabase
{
    /// <summary>Gets whether a database file has been loaded and is ready for queries.</summary>
    bool IsLoaded { get; }

    /// <summary>Gets the file name of the currently loaded database, if any.</summary>
    string? LoadedFileName { get; }

    /// <summary>Gets the UTC timestamp of the last successful sync/pull operation.</summary>
    DateTime? LastSyncTime { get; }

    /// <summary>
    /// Loads a Cashew SQLite database from raw bytes (typically downloaded from Google Drive).
    /// </summary>
    /// <param name="fileBytes">Raw SQLite file content.</param>
    /// <param name="fileName">Original file name for metadata tracking.</param>
    void LoadDatabase(byte[] fileBytes, string fileName);

    /// <summary>Exports the currently loaded database as a raw byte array.</summary>
    byte[] ExportDatabase();

    /// <summary>
    /// Retrieves a paginated, filtered list of transactions ordered by date descending.
    /// </summary>
    List<Transaction> GetTransactions(int page, int pageSize,
        string? walletFk = null, string? categoryFk = null,
        DateTime? startDate = null, DateTime? endDate = null,
        bool? income = null);

    /// <summary>Returns the total count of transactions matching the given filters.</summary>
    int GetTransactionCount(string? walletFk = null, string? categoryFk = null,
        DateTime? startDate = null, DateTime? endDate = null,
        bool? income = null);

    /// <summary>Retrieves a single transaction by its primary key.</summary>
    /// <returns>The transaction, or <c>null</c> if not found.</returns>
    Transaction? GetTransaction(string transactionPk);

    /// <summary>Inserts a new transaction into the database.</summary>
    /// <returns>The created transaction (with generated PK).</returns>
    Transaction CreateTransaction(Transaction transaction);

    /// <summary>Inserts multiple transactions in a single SQLite transaction (batch).</summary>
    List<Transaction> CreateTransactions(List<Transaction> transactions);

    /// <summary>Deletes a transaction and records a delete-log entry for sync.</summary>
    /// <returns><c>true</c> if the transaction existed and was deleted.</returns>
    bool DeleteTransaction(string transactionPk);

    /// <summary>Deletes multiple transactions and records delete-log entries for each.</summary>
    /// <returns>The number of transactions actually deleted.</returns>
    int DeleteTransactions(List<string> transactionPks);

    /// <summary>Retrieves all categories ordered by their display order.</summary>
    List<Category> GetCategories();

    /// <summary>Retrieves a single category by its primary key.</summary>
    Category? GetCategory(string categoryPk);

    /// <summary>Retrieves all wallets ordered by their display order.</summary>
    List<Wallet> GetWallets();

    /// <summary>Retrieves a single wallet by its primary key.</summary>
    Wallet? GetWallet(string walletPk);
}
