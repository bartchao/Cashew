using CashewAPI.Models.ApiModels;
using CashewAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CashewAPI.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sync").WithTags("Sync");

        group.MapPost("/pull", async (ICashewDatabase db, IGoogleDriveService driveService, [FromBody] SyncPullRequest? request) =>
        {
            try
            {
                var (fileBytes, fileName) = await driveService.DownloadLatestCashewDb(request?.FileId);
                db.LoadDatabase(fileBytes, fileName);

                var transactionCount = db.GetTransactionCount();

                return Results.Ok(new
                {
                    message = "Database synced from Google Drive.",
                    fileName,
                    transactionCount,
                });
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse
                {
                    Error = "Failed to sync from Google Drive.",
                    Detail = ex.Message,
                }, statusCode: 500);
            }
        }).WithName("SyncPull");

        group.MapPost("/push", async (ICashewDatabase db, IGoogleDriveService driveService) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            try
            {
                var dbBytes = db.ExportDatabase();
                var fileName = $"cashew-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.sql";
                var fileId = await driveService.UploadCashewDb(dbBytes, fileName);

                return Results.Ok(new
                {
                    message = "Database uploaded to Google Drive.",
                    fileId,
                    fileName,
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse
                {
                    Error = "Failed to upload to Google Drive.",
                    Detail = ex.Message,
                }, statusCode: 500);
            }
        }).WithName("SyncPush");

        group.MapGet("/status", (ICashewDatabase db) =>
        {
            return Results.Ok(new SyncStatusResponse
            {
                DatabaseLoaded = db.IsLoaded,
                FileName = db.LoadedFileName,
                LastSyncTime = db.LastSyncTime,
                TransactionCount = db.IsLoaded ? db.GetTransactionCount() : 0,
            });
        }).WithName("SyncStatus");
    }
}
