using CashewAPI.Models;

namespace CashewAPI.Services;

public interface ICashewDatabase
{
    bool IsLoaded { get; }
    string? LoadedFileName { get; }
    DateTime? LastSyncTime { get; }

    void LoadDatabase(byte[] fileBytes, string fileName);
    byte[] ExportDatabase();

    List<Transaction> GetTransactions(int page, int pageSize,
        string? walletFk = null, string? categoryFk = null,
        DateTime? startDate = null, DateTime? endDate = null,
        bool? income = null);
    int GetTransactionCount(string? walletFk = null, string? categoryFk = null,
        DateTime? startDate = null, DateTime? endDate = null,
        bool? income = null);
    Transaction? GetTransaction(string transactionPk);
    Transaction CreateTransaction(Transaction transaction);
    List<Transaction> CreateTransactions(List<Transaction> transactions);
    bool DeleteTransaction(string transactionPk);
    int DeleteTransactions(List<string> transactionPks);

    List<Category> GetCategories();
    Category? GetCategory(string categoryPk);

    List<Wallet> GetWallets();
    Wallet? GetWallet(string walletPk);
}
