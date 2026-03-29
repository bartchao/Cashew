using System.ComponentModel.DataAnnotations;

namespace CashewAPI.Models.ApiModels;

public class CreateTransactionRequest
{
    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public double Amount { get; set; }

    [Required]
    public string CategoryFk { get; set; } = string.Empty;

    [Required]
    public bool Income { get; set; }

    [MaxLength(500)]
    public string Note { get; set; } = "";

    public string WalletFk { get; set; } = "0";

    public DateTime? DateCreated { get; set; }

    public string? SubCategoryFk { get; set; }

    public int? Type { get; set; }

    public bool Paid { get; set; }

    public string? ObjectiveFk { get; set; }
}

public class BatchCreateTransactionRequest
{
    [Required]
    public List<CreateTransactionRequest> Transactions { get; set; } = [];
}

public class BatchDeleteRequest
{
    [Required]
    public List<string> TransactionPks { get; set; } = [];
}
