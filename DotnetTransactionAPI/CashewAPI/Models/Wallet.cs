// Wallet.cs — Domain model for wallets (accounts) in the Cashew budget app.
// Maps directly to the "wallets" table in the Cashew SQLite database.

namespace CashewAPI.Models;

/// <summary>
/// Represents a wallet or financial account that holds transactions.
/// The default wallet uses PK "0".
/// </summary>
public class Wallet
{
    /// <summary>UUID v4 primary key for this wallet. "0" denotes the default wallet.</summary>
    public string WalletPk { get; set; } = string.Empty;

    /// <summary>Display name of the wallet.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex colour string used for UI display.</summary>
    public string? Colour { get; set; }

    /// <summary>Material icon name for the wallet.</summary>
    public string? IconName { get; set; }

    /// <summary>Date the wallet was created.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Timestamp of the last modification.</summary>
    public DateTime? DateTimeModified { get; set; }

    /// <summary>Sort order for UI display.</summary>
    public int Order { get; set; }

    /// <summary>ISO 4217 currency code (e.g., "USD", "EUR").</summary>
    public string? Currency { get; set; }

    /// <summary>Custom currency formatting pattern.</summary>
    public string? CurrencyFormat { get; set; }

    /// <summary>Number of decimal places for currency display.</summary>
    public int Decimals { get; set; } = 2;

    /// <summary>Configuration for the home-page widget display mode.</summary>
    public string? HomePageWidgetDisplay { get; set; }
}
