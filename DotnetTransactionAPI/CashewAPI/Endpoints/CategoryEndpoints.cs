using CashewAPI.Models.ApiModels;
using CashewAPI.Services;

namespace CashewAPI.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", (ICashewDatabase db) =>
        {
            if (!db.IsLoaded)
                return Results.Json(new ErrorResponse { Error = "Database not loaded. Call POST /api/sync/pull first." }, statusCode: 503);

            var categories = db.GetCategories();
            return Results.Ok(categories);
        }).WithName("GetCategories");

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
