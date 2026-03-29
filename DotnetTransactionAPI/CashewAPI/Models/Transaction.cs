// Transaction.cs — Domain model representing a financial transaction in the Cashew budget app.
// Maps directly to the "transactions" table in the Cashew SQLite database.

namespace CashewAPI.Models;

/// <summary>
/// Represents a single financial transaction (expense or income) stored in the Cashew database.
/// All dates are persisted as Unix milliseconds and converted at the data-access layer.
/// </summary>
public class Transaction
{
    /// <summary>UUID v4 primary key for this transaction.</summary>
    public string TransactionPk { get; set; } = string.Empty;

    /// <summary>Foreign key to a paired/linked transaction (e.g., transfer counterpart).</summary>
    public string? PairedTransactionFk { get; set; }

    /// <summary>Display name or description of the transaction.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Monetary amount. Positive for income, negative for expenses.</summary>
    public double Amount { get; set; }

    /// <summary>Optional user note attached to the transaction.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent category.</summary>
    public string CategoryFk { get; set; } = string.Empty;

    /// <summary>Foreign key to an optional subcategory.</summary>
    public string? SubCategoryFk { get; set; }

    /// <summary>Foreign key to the wallet. Defaults to "0" (primary wallet).</summary>
    public string WalletFk { get; set; } = "0";

    /// <summary>Date the transaction occurred.</summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp of the last modification.</summary>
    public DateTime? DateTimeModified { get; set; }

    /// <summary>Original due date for scheduled/recurring transactions.</summary>
    public DateTime? OriginalDateDue { get; set; }

    /// <summary>Whether this transaction is income (<c>true</c>) or an expense (<c>false</c>).</summary>
    public bool Income { get; set; }

    /// <summary>Length of the recurrence period (used with <see cref="Reoccurrence"/>).</summary>
    public int? PeriodLength { get; set; }

    /// <summary>Recurrence type identifier for scheduled transactions.</summary>
    public int? Reoccurrence { get; set; }

    /// <summary>End date for a recurring transaction series.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether to send a notification for upcoming scheduled transactions.</summary>
    public bool? UpcomingTransactionNotification { get; set; } = true;

    /// <summary>Transaction type identifier (e.g., regular, subscription, upcoming).</summary>
    public int? Type { get; set; }

    /// <summary>Whether this transaction has been paid/settled.</summary>
    public bool Paid { get; set; }

    /// <summary>Flag indicating if a future recurrence has already been created.</summary>
    public bool? CreatedAnotherFutureTransaction { get; set; }

    /// <summary>Whether payment was skipped for this occurrence.</summary>
    public bool SkipPaid { get; set; }

    /// <summary>How the transaction was added (e.g., 5 = appLink).</summary>
    public int? MethodAdded { get; set; }

    /// <summary>Email of the user who owns this transaction (for shared budgets).</summary>
    public string? TransactionOwnerEmail { get; set; }

    /// <summary>Email of the original owner before sharing.</summary>
    public string? TransactionOriginalOwnerEmail { get; set; }

    /// <summary>Current shared key for collaborative budget syncing.</summary>
    public string? SharedKey { get; set; }

    /// <summary>Previous shared key (used during key rotation).</summary>
    public string? SharedOldKey { get; set; }

    /// <summary>Status of the shared transaction (e.g., pending, accepted).</summary>
    public int? SharedStatus { get; set; }

    /// <summary>Timestamp when the shared status was last updated.</summary>
    public DateTime? SharedDateUpdated { get; set; }

    /// <summary>Reference to the shared budget this transaction belongs to.</summary>
    public string? SharedReferenceBudgetPk { get; set; }

    /// <summary>Foreign key to an associated savings objective/goal.</summary>
    public string? ObjectiveFk { get; set; }

    /// <summary>Foreign key to an associated loan objective.</summary>
    public string? ObjectiveLoanFk { get; set; }

    /// <summary>Comma-separated budget PKs from which this transaction is excluded.</summary>
    public string? BudgetFksExclude { get; set; }
}
