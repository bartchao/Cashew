using CashewAPI.Endpoints;
using CashewAPI.Models;
using CashewAPI.Models.ApiModels;
using CashewAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace CashewAPI.Tests.Endpoints;

public class TransactionEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDir;

    public TransactionEndpointsTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"CashewAPIEndpointTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);

        // Create and populate test database
        CreateTestDatabase();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DatabaseDirectory"] = _testDir,
                        ["ApiKey"] = "",
                        ["GoogleDrive:CredentialPath"] = "",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Remove all GoogleDriveService registrations and replace with a fake
                    var descriptors = services.Where(d => d.ServiceType == typeof(IGoogleDriveService)).ToList();
                    foreach (var descriptor in descriptors)
                        services.Remove(descriptor);

                    services.AddSingleton<IGoogleDriveService, FakeGoogleDriveService>();

                    // Also remove and re-add CashewDatabase to pick up test config
                    var dbDescriptors = services.Where(d => d.ServiceType == typeof(ICashewDatabase)).ToList();
                    foreach (var descriptor in dbDescriptors)
                        services.Remove(descriptor);

                    services.AddSingleton<ICashewDatabase>(sp =>
                    {
                        var config = sp.GetRequiredService<IConfiguration>();
                        return new CashewDatabase(config);
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    private void CreateTestDatabase()
    {
        var dbPath = Path.Combine(_testDir, "cashew.db");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS categories (
                category_pk TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                colour TEXT,
                icon_name TEXT,
                emoji_icon_name TEXT,
                date_created INTEGER NOT NULL,
                date_time_modified INTEGER,
                ""order"" INTEGER NOT NULL DEFAULT 0,
                income INTEGER NOT NULL DEFAULT 0,
                method_added INTEGER,
                main_category_pk TEXT
            );

            CREATE TABLE IF NOT EXISTS wallets (
                wallet_pk TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                colour TEXT,
                icon_name TEXT,
                date_created INTEGER NOT NULL,
                date_time_modified INTEGER,
                ""order"" INTEGER NOT NULL DEFAULT 0,
                currency TEXT,
                currency_format TEXT,
                decimals INTEGER NOT NULL DEFAULT 2,
                home_page_widget_display TEXT
            );

            CREATE TABLE IF NOT EXISTS transactions (
                transaction_pk TEXT NOT NULL PRIMARY KEY,
                paired_transaction_fk TEXT,
                name TEXT NOT NULL,
                amount REAL NOT NULL,
                note TEXT NOT NULL DEFAULT '',
                category_fk TEXT NOT NULL,
                sub_category_fk TEXT,
                wallet_fk TEXT NOT NULL DEFAULT '0',
                date_created INTEGER NOT NULL,
                date_time_modified INTEGER,
                original_date_due INTEGER,
                income INTEGER NOT NULL DEFAULT 0,
                period_length INTEGER,
                reoccurrence INTEGER,
                end_date INTEGER,
                upcoming_transaction_notification INTEGER DEFAULT 1,
                type INTEGER,
                paid INTEGER NOT NULL DEFAULT 0,
                created_another_future_transaction INTEGER,
                skip_paid INTEGER NOT NULL DEFAULT 0,
                method_added INTEGER,
                transaction_owner_email TEXT,
                transaction_original_owner_email TEXT,
                shared_key TEXT,
                shared_old_key TEXT,
                shared_status INTEGER,
                shared_date_updated INTEGER,
                shared_reference_budget_pk TEXT,
                objective_fk TEXT,
                objective_loan_fk TEXT,
                budget_fks_exclude TEXT
            );

            CREATE TABLE IF NOT EXISTS delete_logs (
                delete_log_pk TEXT NOT NULL PRIMARY KEY,
                entry_pk TEXT NOT NULL,
                type INTEGER NOT NULL,
                date_time_modified INTEGER NOT NULL
            );

            INSERT INTO wallets (wallet_pk, name, date_created, ""order"", decimals)
            VALUES ('0', 'Default Wallet', 1700000000000, 0, 2);

            INSERT INTO categories (category_pk, name, date_created, ""order"", income)
            VALUES ('cat-food', 'Food', 1700000000000, 1, 0);

            INSERT INTO categories (category_pk, name, date_created, ""order"", income)
            VALUES ('cat-salary', 'Salary', 1700000000000, 2, 1);

            INSERT INTO transactions (transaction_pk, name, amount, note, category_fk, wallet_fk, date_created, income, paid)
            VALUES ('txn-1', 'Grocery', -50.00, 'Weekly groceries', 'cat-food', '0', 1700100000000, 0, 0);

            INSERT INTO transactions (transaction_pk, name, amount, note, category_fk, wallet_fk, date_created, income, paid)
            VALUES ('txn-2', 'Monthly Salary', 5000.00, '', 'cat-salary', '0', 1700200000000, 1, 1);
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetTransactions_ReturnsOk_WithPaginatedData()
    {
        var response = await _client.GetAsync("/api/transactions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<TransactionResponse>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task GetTransaction_ExistingPk_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/transactions/txn-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal("Grocery", result.Name);
    }

    [Fact]
    public async Task GetTransaction_NonExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/transactions/non-existing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_ValidRequest_ReturnsCreated()
    {
        var request = new CreateTransactionRequest
        {
            Name = "New Expense",
            Amount = 25.00,
            CategoryFk = "cat-food",
            Income = false,
            Note = "Test",
        };

        var response = await _client.PostAsJsonAsync("/api/transactions", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal("New Expense", result.Name);
        Assert.Equal(-25.00, result.Amount); // Expense should be negative
        Assert.False(result.Income);
    }

    [Fact]
    public async Task CreateTransaction_InvalidCategory_ReturnsBadRequest()
    {
        var request = new CreateTransactionRequest
        {
            Name = "Bad Transaction",
            Amount = 10.00,
            CategoryFk = "non-existing-cat",
            Income = false,
        };

        var response = await _client.PostAsJsonAsync("/api/transactions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateTransactionRequest
        {
            Name = "",
            Amount = 10.00,
            CategoryFk = "cat-food",
            Income = false,
        };

        var response = await _client.PostAsJsonAsync("/api/transactions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTransaction_ExistingPk_ReturnsOk()
    {
        var response = await _client.DeleteAsync("/api/transactions/txn-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify it's actually deleted
        var getResponse = await _client.GetAsync("/api/transactions/txn-1");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTransaction_NonExisting_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/transactions/non-existing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<Category>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetWallets_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/wallets");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<Wallet>>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetSyncStatus_ReturnsStatus()
    {
        var response = await _client.GetAsync("/api/sync/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SyncStatusResponse>();
        Assert.NotNull(result);
        Assert.True(result.DatabaseLoaded);
    }

    [Fact]
    public async Task BatchCreateTransactions_ValidRequest_ReturnsCreated()
    {
        var request = new BatchCreateTransactionRequest
        {
            Transactions =
            [
                new CreateTransactionRequest
                {
                    Name = "Batch 1",
                    Amount = 10.00,
                    CategoryFk = "cat-food",
                    Income = false,
                },
                new CreateTransactionRequest
                {
                    Name = "Batch 2",
                    Amount = 200.00,
                    CategoryFk = "cat-salary",
                    Income = true,
                },
            ],
        };

        var response = await _client.PostAsJsonAsync("/api/transactions/batch", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    private class FakeGoogleDriveService : IGoogleDriveService
    {
        public Task<(byte[] fileBytes, string fileName)> DownloadLatestCashewDb(string? fileId = null)
            => throw new NotImplementedException("Google Drive not configured for tests");

        public Task<string> UploadCashewDb(byte[] fileBytes, string fileName)
            => throw new NotImplementedException("Google Drive not configured for tests");
    }
}
