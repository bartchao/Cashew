// CreateTransactionRequest.cs — API request DTOs for creating and deleting transactions.

using System.ComponentModel.DataAnnotations;

namespace CashewAPI.Models.ApiModels;

/// <summary>
/// Request body for creating a single transaction via the API.
/// The server generates the primary key and timestamps automatically.
/// </summary>
public class CreateTransactionRequest
{
    /// <summary>Display name or description of the transaction (max 250 chars).</summary>
    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Monetary amount. Sign is normalised by the server based on <see cref="Income"/>.</summary>
    [Required]
    public double Amount { get; set; }

    /// <summary>Primary key of the category this transaction belongs to.</summary>
    [Required]
    public string CategoryFk { get; set; } = string.Empty;

    /// <summary>Whether this transaction is income (<c>true</c>) or an expense (<c>false</c>).</summary>
    [Required]
    public bool Income { get; set; }

    /// <summary>Optional note (max 500 chars).</summary>
    [MaxLength(500)]
    public string Note { get; set; } = "";

    /// <summary>Wallet PK to assign. Defaults to "0" (primary wallet).</summary>
    public string WalletFk { get; set; } = "0";

    /// <summary>Transaction date. Defaults to UTC now if omitted.</summary>
    public DateTime? DateCreated { get; set; }

    /// <summary>Optional subcategory PK (must belong to <see cref="CategoryFk"/>).</summary>
    public string? SubCategoryFk { get; set; }

    /// <summary>Transaction type identifier.</summary>
    public int? Type { get; set; }

    /// <summary>Whether this transaction has been paid/settled.</summary>
    public bool Paid { get; set; }

    /// <summary>Optional savings objective/goal PK to associate with.</summary>
    public string? ObjectiveFk { get; set; }
}

/// <summary>
/// Request body for creating multiple transactions in a single batch (max 1000).
/// </summary>
public class BatchCreateTransactionRequest
{
    /// <summary>List of transactions to create.</summary>
    [Required]
    public List<CreateTransactionRequest> Transactions { get; set; } = [];
}

/// <summary>
/// Request body for deleting multiple transactions by their primary keys (max 1000).
/// </summary>
public class BatchDeleteRequest
{
    /// <summary>List of transaction primary keys to delete.</summary>
    [Required]
    public List<string> TransactionPks { get; set; } = [];
}
