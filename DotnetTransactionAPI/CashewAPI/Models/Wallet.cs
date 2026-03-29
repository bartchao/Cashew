namespace CashewAPI.Models;

public class Wallet
{
    public string WalletPk { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Colour { get; set; }
    public string? IconName { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateTimeModified { get; set; }
    public int Order { get; set; }
    public string? Currency { get; set; }
    public string? CurrencyFormat { get; set; }
    public int Decimals { get; set; } = 2;
    public string? HomePageWidgetDisplay { get; set; }
}
