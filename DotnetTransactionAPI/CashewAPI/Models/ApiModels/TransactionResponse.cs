// TransactionResponse.cs — API response DTOs shared across the CashewAPI endpoints.

namespace CashewAPI.Models.ApiModels;

/// <summary>
/// Simplified transaction view returned by the API.
/// Excludes internal/shared-budget fields that are not relevant to API consumers.
/// </summary>
public class TransactionResponse
{
    /// <summary>UUID v4 primary key.</summary>
    public string TransactionPk { get; set; } = string.Empty;

    /// <summary>Display name of the transaction.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Monetary amount (positive = income, negative = expense).</summary>
    public double Amount { get; set; }

    /// <summary>User note attached to the transaction.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent category.</summary>
    public string CategoryFk { get; set; } = string.Empty;

    /// <summary>Foreign key to the subcategory, if any.</summary>
    public string? SubCategoryFk { get; set; }

    /// <summary>Foreign key to the wallet.</summary>
    public string WalletFk { get; set; } = "0";

    /// <summary>Date the transaction occurred.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Timestamp of the last modification.</summary>
    public DateTime? DateTimeModified { get; set; }

    /// <summary>Whether this is an income transaction.</summary>
    public bool Income { get; set; }

    /// <summary>Transaction type identifier.</summary>
    public int? Type { get; set; }

    /// <summary>Whether the transaction has been paid/settled.</summary>
    public bool Paid { get; set; }

    /// <summary>Associated savings objective/goal PK.</summary>
    public string? ObjectiveFk { get; set; }

    /// <summary>
    /// Maps a domain <see cref="Transaction"/> to an API-facing <see cref="TransactionResponse"/>.
    /// </summary>
    public static TransactionResponse FromTransaction(Transaction t) => new()
    {
        TransactionPk = t.TransactionPk,
        Name = t.Name,
        Amount = t.Amount,
        Note = t.Note,
        CategoryFk = t.CategoryFk,
        SubCategoryFk = t.SubCategoryFk,
        WalletFk = t.WalletFk,
        DateCreated = t.DateCreated,
        DateTimeModified = t.DateTimeModified,
        Income = t.Income,
        Type = t.Type,
        Paid = t.Paid,
        ObjectiveFk = t.ObjectiveFk,
    };
}

/// <summary>
/// Generic paginated response wrapper used by list endpoints.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>Items on the current page.</summary>
    public List<T> Data { get; set; } = [];

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Maximum items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of matching items across all pages.</summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Response returned by the <c>GET /api/sync/status</c> endpoint.
/// </summary>
public class SyncStatusResponse
{
    /// <summary>Whether a Cashew database is currently loaded in memory.</summary>
    public bool DatabaseLoaded { get; set; }

    /// <summary>Name of the loaded database file, if any.</summary>
    public string? FileName { get; set; }

    /// <summary>UTC timestamp of the last successful sync/pull.</summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>Total number of transactions in the loaded database.</summary>
    public int TransactionCount { get; set; }
}

/// <summary>
/// Optional request body for <c>POST /api/sync/pull</c> to specify a Google Drive file ID.
/// </summary>
public class SyncPullRequest
{
    /// <summary>Google Drive file ID. If omitted, the latest Cashew DB file is used.</summary>
    public string? FileId { get; set; }
}

/// <summary>
/// Standard error response returned by all API endpoints on failure.
/// </summary>
public class ErrorResponse
{
    /// <summary>Human-readable error message.</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>Optional additional detail (e.g., exception message).</summary>
    public string? Detail { get; set; }
}
