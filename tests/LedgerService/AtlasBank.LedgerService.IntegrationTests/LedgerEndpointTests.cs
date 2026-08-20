using AtlasBank.LedgerService.Data;
using AtlasBank.LedgerService.Domain.Entities;
using AtlasBank.LedgerService.Domain.Enums;
using AtlasBank.LedgerService.Features.Ledger;
using AtlasBank.LedgerService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtlasBank.LedgerService.IntegrationTests;

public class LedgerEndpointTests : IClassFixture<LedgerServiceFactory>
{
    private readonly HttpClient _client;
    private readonly LedgerServiceFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public LedgerEndpointTests(LedgerServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        var token = TestJwtTokenGenerator.GenerateToken(Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task SeedAsync(Guid accountId, LedgerEntryType type, decimal amount, Guid? transactionId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();

        db.LedgerEntries.Add(LedgerEntry.Create(
            transactionId ?? Guid.NewGuid(), accountId, type, amount, "USD", "REF", "Deposit", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByAccount_ReturnsEntriesForThatAccount()
    {
        var accountId = Guid.NewGuid();
        await SeedAsync(accountId, LedgerEntryType.Credit, 100m);
        await SeedAsync(accountId, LedgerEntryType.Debit, 30m);
        await SeedAsync(Guid.NewGuid(), LedgerEntryType.Credit, 999m); // different account

        var response = await _client.GetAsync($"/api/ledger/account/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<LedgerEntryResponse>>(JsonOptions);
        results.Should().HaveCount(2);
        results!.Should().AllSatisfy(e => e.AccountId.Should().Be(accountId));
    }

    [Fact]
    public async Task GetByAccount_UnknownAccount_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/ledger/account/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<LedgerEntryResponse>>(JsonOptions);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAccount_NoToken_Returns401()
    {
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/ledger/account/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBalance_SumsCreditsMinusDebits()
    {
        var accountId = Guid.NewGuid();
        await SeedAsync(accountId, LedgerEntryType.Credit, 500m);
        await SeedAsync(accountId, LedgerEntryType.Credit, 200m);
        await SeedAsync(accountId, LedgerEntryType.Debit, 150m);

        var response = await _client.GetAsync($"/api/ledger/account/{accountId}/balance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LedgerBalanceResponse>(JsonOptions);
        result!.AccountId.Should().Be(accountId);
        result.Balance.Should().Be(550m);
    }

    [Fact]
    public async Task GetBalance_NoEntries_ReturnsZero()
    {
        var response = await _client.GetAsync($"/api/ledger/account/{Guid.NewGuid()}/balance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LedgerBalanceResponse>(JsonOptions);
        result!.Balance.Should().Be(0m);
    }
}
