// WalletEndpoints.cs — Minimal API endpoint definitions for read-only wallet access.

using CashewAPI.Models.ApiModels;
using CashewAPI.Services;

namespace CashewAPI.Endpoints;

/// <summary>
/// Defines the <c>/api/wallets</c> endpoint group for listing and retrieving wallets.
/// Wallets are read-only through the API; they are managed in the Cashew Flutter app.
/// </summary>
public static class WalletEndpoints
{
    /// <summary>
    /// Registers all wallet-related routes on the application.
    /// </summary>
    public static void MapWalletEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/wallets").WithTags("Wallets");

        // GET /api/wallets — List all wallets ordered by display order
        group.MapGet("/", (ICashewDatabase db) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            var wallets = db.GetWallets();
            return Results.Ok(wallets);
        }).WithName("GetWallets");

        // GET /api/wallets/{pk} — Retrieve a single wallet by primary key
        group.MapGet("/{pk}", (ICashewDatabase db, string pk) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            var wallet = db.GetWallet(pk);
            if (wallet == null)
                return Results.NotFound(new ErrorResponse { Error = "Wallet not found." });

            return Results.Ok(wallet);
        }).WithName("GetWallet");
    }
}
