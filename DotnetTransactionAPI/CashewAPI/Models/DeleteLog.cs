namespace CashewAPI.Models;

public class DeleteLog
{
    public string DeleteLogPk { get; set; } = string.Empty;
    public string EntryPk { get; set; } = string.Empty;
    public int Type { get; set; }
    public DateTime DateTimeModified { get; set; }
}

public static class DeleteLogType
{
    public const int TransactionWallet = 0;
    public const int TransactionCategory = 1;
    public const int Budget = 2;
    public const int CategoryBudgetLimit = 3;
    public const int Transaction = 4;
    public const int TransactionAssociatedTitle = 5;
    public const int ScannerTemplate = 6;
    public const int Objective = 7;
}
