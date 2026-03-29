using CashewAPI.Models.ApiModels;
using CashewAPI.Services;

namespace CashewAPI.Endpoints;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/wallets").WithTags("Wallets");

        group.MapGet("/", (ICashewDatabase db) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            var wallets = db.GetWallets();
            return Results.Ok(wallets);
        }).WithName("GetWallets");

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
