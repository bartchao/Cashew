// CategoryEndpoints.cs — Minimal API endpoint definitions for read-only category access.

using CashewAPI.Models.ApiModels;
using CashewAPI.Services;

namespace CashewAPI.Endpoints;

/// <summary>
/// Defines the <c>/api/categories</c> endpoint group for listing and retrieving categories.
/// Categories are read-only through the API; they are managed in the Cashew Flutter app.
/// </summary>
public static class CategoryEndpoints
{
    /// <summary>
    /// Registers all category-related routes on the application.
    /// </summary>
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        // GET /api/categories — List all categories ordered by display order
        group.MapGet("/", (ICashewDatabase db) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            var categories = db.GetCategories();
            return Results.Ok(categories);
        }).WithName("GetCategories");

        // GET /api/categories/{pk} — Retrieve a single category by primary key
        group.MapGet("/{pk}", (ICashewDatabase db, string pk) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            var category = db.GetCategory(pk);
            if (category == null)
                return Results.NotFound(new ErrorResponse { Error = "Category not found." });

            return Results.Ok(category);
        }).WithName("GetCategory");
    }
}
