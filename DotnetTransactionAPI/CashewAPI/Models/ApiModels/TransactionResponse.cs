namespace CashewAPI.Models.ApiModels;

public class TransactionResponse
{
    public string TransactionPk { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Note { get; set; } = string.Empty;
    public string CategoryFk { get; set; } = string.Empty;
    public string? SubCategoryFk { get; set; }
    public string WalletFk { get; set; } = "0";
    public DateTime DateCreated { get; set; }
    public DateTime? DateTimeModified { get; set; }
    public bool Income { get; set; }
    public int? Type { get; set; }
    public bool Paid { get; set; }
    public string? ObjectiveFk { get; set; }

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

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class SyncStatusResponse
{
    public bool DatabaseLoaded { get; set; }
    public string? FileName { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int TransactionCount { get; set; }
}

public class SyncPullRequest
{
    public string? FileId { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
