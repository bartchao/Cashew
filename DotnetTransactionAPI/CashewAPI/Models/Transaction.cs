namespace CashewAPI.Models;

public class Transaction
{
    public string TransactionPk { get; set; } = string.Empty;
    public string? PairedTransactionFk { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Note { get; set; } = string.Empty;
    public string CategoryFk { get; set; } = string.Empty;
    public string? SubCategoryFk { get; set; }
    public string WalletFk { get; set; } = "0";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateTimeModified { get; set; }
    public DateTime? OriginalDateDue { get; set; }
    public bool Income { get; set; }
    public int? PeriodLength { get; set; }
    public int? Reoccurrence { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? UpcomingTransactionNotification { get; set; } = true;
    public int? Type { get; set; }
    public bool Paid { get; set; }
    public bool? CreatedAnotherFutureTransaction { get; set; }
    public bool SkipPaid { get; set; }
    public int? MethodAdded { get; set; }
    public string? TransactionOwnerEmail { get; set; }
    public string? TransactionOriginalOwnerEmail { get; set; }
    public string? SharedKey { get; set; }
    public string? SharedOldKey { get; set; }
    public int? SharedStatus { get; set; }
    public DateTime? SharedDateUpdated { get; set; }
    public string? SharedReferenceBudgetPk { get; set; }
    public string? ObjectiveFk { get; set; }
    public string? ObjectiveLoanFk { get; set; }
    public string? BudgetFksExclude { get; set; }
}
