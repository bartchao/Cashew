// DeleteLog.cs — Domain model for soft-delete tracking in the Cashew budget app.
// The Cashew Flutter app uses delete logs to synchronise deletions across devices.

namespace CashewAPI.Models;

/// <summary>
/// Records the deletion of an entity so that sync clients can replicate the removal.
/// Maps to the "delete_logs" table in the Cashew SQLite database.
/// </summary>
public class DeleteLog
{
    /// <summary>UUID v4 primary key for this log entry.</summary>
    public string DeleteLogPk { get; set; } = string.Empty;

    /// <summary>Primary key of the deleted entity.</summary>
    public string EntryPk { get; set; } = string.Empty;

    /// <summary>Entity type that was deleted. See <see cref="DeleteLogType"/> constants.</summary>
    public int Type { get; set; }

    /// <summary>Timestamp when the deletion occurred.</summary>
    public DateTime DateTimeModified { get; set; }
}

/// <summary>
/// Constants identifying the type of entity recorded in a <see cref="DeleteLog"/>.
/// </summary>
public static class DeleteLogType
{
    /// <summary>A wallet was deleted (legacy transaction-wallet type).</summary>
    public const int TransactionWallet = 0;

    /// <summary>A category was deleted (legacy transaction-category type).</summary>
    public const int TransactionCategory = 1;

    /// <summary>A budget was deleted.</summary>
    public const int Budget = 2;

    /// <summary>A per-category budget limit was deleted.</summary>
    public const int CategoryBudgetLimit = 3;

    /// <summary>A transaction was deleted.</summary>
    public const int Transaction = 4;

    /// <summary>A transaction-associated title was deleted.</summary>
    public const int TransactionAssociatedTitle = 5;

    /// <summary>A receipt/scanner template was deleted.</summary>
    public const int ScannerTemplate = 6;

    /// <summary>A savings objective/goal was deleted.</summary>
    public const int Objective = 7;
}
