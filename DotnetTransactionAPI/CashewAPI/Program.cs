// Program.cs — Entry point for the CashewAPI .NET Minimal API application.
// Configures services, middleware, and maps all API endpoint groups.

using CashewAPI.Endpoints;
using CashewAPI.Middleware;
using CashewAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICashewDatabase, CashewDatabase>();
builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();

// Map API endpoints
app.MapTransactionEndpoints();
app.MapCategoryEndpoints();
app.MapWalletEndpoints();
app.MapSyncEndpoints();

app.Run();

// Enable WebApplicationFactory<Program> in tests
/// <summary>
/// Partial class declaration to allow <c>WebApplicationFactory&lt;Program&gt;</c>
/// to be used in integration tests.
/// </summary>
public partial class Program { }
