// TransactionEndpoints.cs — Minimal API endpoint definitions for transaction CRUD operations.
// Supports single and batch create/delete, with paginated listing and filtering.

using CashewAPI.Models;
using CashewAPI.Models.ApiModels;
using CashewAPI.Services;

namespace CashewAPI.Endpoints;

/// <summary>
/// Defines the <c>/api/transactions</c> endpoint group for managing financial transactions.
/// All endpoints require the database to be loaded via <c>POST /api/sync/pull</c> first.
/// </summary>
public static class TransactionEndpoints
{
    /// <summary>Maximum allowed absolute value for a transaction amount.</summary>
    private const double MaxAmount = 999_999_999_999;

    /// <summary>
    /// Registers all transaction-related routes on the application.
    /// </summary>
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions");

        group.MapGet("/", GetTransactions).WithName("GetTransactions");
        group.MapGet("/{pk}", GetTransaction).WithName("GetTransaction");
        group.MapPost("/", CreateTransaction).WithName("CreateTransaction");
        group.MapPost("/batch", BatchCreateTransactions).WithName("BatchCreateTransactions");
        group.MapDelete("/{pk}", DeleteTransaction).WithName("DeleteTransaction");
        group.MapPost("/batch-delete", BatchDeleteTransactions).WithName("BatchDeleteTransactions");
    }

    /// <summary>
    /// GET /api/transactions — Returns a paginated, filterable list of transactions.
    /// </summary>
    private static IResult GetTransactions(
        ICashewDatabase db,
        int page = 1,
        int pageSize = 20,
        string? walletFk = null,
        string? categoryFk = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? income = null)
    {
        if (!db.IsLoaded)
            return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

        // Clamp pagination parameters to valid range
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var transactions = db.GetTransactions(page, pageSize, walletFk, categoryFk, startDate, endDate, income);
        var totalCount = db.GetTransactionCount(walletFk, categoryFk, startDate, endDate, income);

        return Results.Ok(new PaginatedResponse<TransactionResponse>
        {
            Data = transactions.Select(TransactionResponse.FromTransaction).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    /// <summary>
    /// GET /api/transactions/{pk} — Returns a single transaction by primary key.
    /// </summary>
    private static IResult GetTransaction(ICashewDatabase db, string pk)
    {
        if (!db.IsLoaded)
            return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

        var transaction = db.GetTransaction(pk);
        if (transaction == null)
            return Results.NotFound(new ErrorResponse { Error = "Transaction not found." });

        return Results.Ok(TransactionResponse.FromTransaction(transaction));
    }

    /// <summary>
    /// POST /api/transactions — Creates a single transaction after validation.
    /// </summary>
    private static IResult CreateTransaction(ICashewDatabase db, CreateTransactionRequest request)
    {
        if (!db.IsLoaded)
            return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

        var validationError = ValidateCreateRequest(db, request);
        if (validationError != null)
            return Results.BadRequest(new ErrorResponse { Error = validationError });

        var transaction = MapToTransaction(request);
        db.CreateTransaction(transaction);

        return Results.Created($"/api/transactions/{transaction.TransactionPk}",
            TransactionResponse.FromTransaction(transaction));
    }

    /// <summary>
    /// POST /api/transactions/batch — Creates up to 1000 transactions in a single batch.
    /// Validates all items before inserting any (all-or-nothing).
    /// </summary>
    private static IResult BatchCreateTransactions(ICashewDatabase db, BatchCreateTransactionRequest request)
    {
        if (!db.IsLoaded)
            return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

        if (request.Transactions.Count == 0)
            return Results.BadRequest(new ErrorResponse { Error = "No transactions provided." });

        if (request.Transactions.Count > 1000)
            return Results.BadRequest(new ErrorResponse { Error = "Maximum 1000 transactions per batch." });

        // Validate every transaction before inserting any
        var transactions = new List<Transaction>();
        for (int i = 0; i < request.Transactions.Count; i++)
        {
            var validationError = ValidateCreateRequest(db, request.Transactions[i]);
            if (validationError != null)
                return Results.BadRequest(new ErrorResponse { Error = $"Transaction[{i}]: {validationError}" });

            transactions.Add(MapToTransaction(request.Transactions[i]));
        }

        db.CreateTransactions(transactions);

        return Results.Created("/api/transactions",
            transactions.Select(TransactionResponse.FromTransaction).ToList());
    }

    /// <summary>
    /// DELETE /api/transactions/{pk} — Deletes a single transaction by primary key.
    /// </summary>
    private static IResult DeleteTransaction(ICashewDatabase db, string pk)
    {
        if (!db.IsLoaded)
            return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

        var deleted = db.DeleteTransaction(pk);
        if (!deleted)
            return Results.NotFound(new ErrorResponse { Error = "Transaction not found." });

        return Results.Ok(new { message = "Transaction deleted.", transactionPk = pk });
    }

    /// <summary>
    /// POST /api/transactions/batch-delete — Deletes up to 1000 transactions by primary key.
    /// </summary>
    private static IResult BatchDeleteTransactions(ICashewDatabase db, BatchDeleteRequest request)
    {
        if (!db.IsLoaded)
            return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

        if (request.TransactionPks.Count == 0)
            return Results.BadRequest(new ErrorResponse { Error = "No transaction PKs provided." });

        if (request.TransactionPks.Count > 1000)
            return Results.BadRequest(new ErrorResponse { Error = "Maximum 1000 transactions per batch." });

        var deletedCount = db.DeleteTransactions(request.TransactionPks);

        return Results.Ok(new { message = $"{deletedCount} transaction(s) deleted.", deletedCount });
    }

    /// <summary>
    /// Validates a <see cref="CreateTransactionRequest"/> against business rules
    /// and ensures referenced categories, subcategories, and wallets exist.
    /// </summary>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    private static string? ValidateCreateRequest(ICashewDatabase db, CreateTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Name is required.";

        if (request.Name.Length > 250)
            return "Name must be 250 characters or less.";

        if (Math.Abs(request.Amount) > MaxAmount)
            return $"Amount must be between -{MaxAmount} and {MaxAmount}.";

        if (request.Note.Length > 500)
            return "Note must be 500 characters or less.";

        if (string.IsNullOrWhiteSpace(request.CategoryFk))
            return "CategoryFk is required.";

        var category = db.GetCategory(request.CategoryFk);
        if (category == null)
            return $"Category '{request.CategoryFk}' not found.";

        // Verify subcategory exists and belongs to the specified parent category
        if (!string.IsNullOrEmpty(request.SubCategoryFk))
        {
            var subCategory = db.GetCategory(request.SubCategoryFk);
            if (subCategory == null)
                return $"SubCategory '{request.SubCategoryFk}' not found.";
            if (subCategory.MainCategoryPk != request.CategoryFk)
                return $"SubCategory '{request.SubCategoryFk}' does not belong to category '{request.CategoryFk}'.";
        }

        // Verify wallet exists (PK "0" is the default wallet and always valid)
        if (!string.IsNullOrEmpty(request.WalletFk) && request.WalletFk != "0")
        {
            var wallet = db.GetWallet(request.WalletFk);
            if (wallet == null)
                return $"Wallet '{request.WalletFk}' not found.";
        }

        return null;
    }

    /// <summary>
    /// Maps an API request DTO to a domain <see cref="Transaction"/> entity,
    /// generating a new UUID primary key and normalising the amount sign.
    /// </summary>
    private static Transaction MapToTransaction(CreateTransactionRequest request)
    {
        return new Transaction
        {
            TransactionPk = Guid.NewGuid().ToString(),
            Name = request.Name,
            // Income amounts are stored as positive, expenses as negative
            Amount = request.Income ? Math.Abs(request.Amount) : -Math.Abs(request.Amount),
            Note = request.Note,
            CategoryFk = request.CategoryFk,
            SubCategoryFk = request.SubCategoryFk,
            WalletFk = request.WalletFk,
            DateCreated = request.DateCreated ?? DateTime.UtcNow,
            DateTimeModified = DateTime.UtcNow,
            Income = request.Income,
            Type = request.Type,
            Paid = request.Paid,
            ObjectiveFk = request.ObjectiveFk,
            MethodAdded = 5, // appLink
        };
    }
}
