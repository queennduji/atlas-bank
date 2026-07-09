using AtlasBank.CardService.Data;
using AtlasBank.CardService.Domain.Entities;
using AtlasBank.CardService.Domain.Enums;
using AtlasBank.CardService.Features.Cards;
using AtlasBank.CardService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtlasBank.CardService.IntegrationTests;

public class CardTests : IClassFixture<CardServiceFactory>
{
    private readonly HttpClient _client;
    private readonly CardServiceFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public CardTests(CardServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        var token = TestJwtTokenGenerator.GenerateToken(Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<CardResponse> IssueCardAsync(
        Guid? accountId = null,
        CardType type = CardType.Debit,
        decimal limit = 1000m)
    {
        var body = new IssueCardRequest(
            accountId ?? FakeAccountServiceClient.AccountId,
            type,
            limit);

        var response = await _client.PostAsJsonAsync("/api/cards", body, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CardResponse>(JsonOptions))!;
    }

    private async Task<Card> SeedCardAsync(CardStatus? status = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CardDbContext>();

        var card = Card.Issue(
            FakeAccountServiceClient.AccountId,
            FakeAccountServiceClient.CustomerId,
            "Seeded User",
            CardType.Debit,
            500m);

        if (status == CardStatus.Frozen) card.Freeze();
        if (status == CardStatus.Cancelled) card.Cancel();

        db.Cards.Add(card);
        await db.SaveChangesAsync();
        return card;
    }

    // issue card

    [Fact]
    public async Task IssueCard_ValidRequest_Returns201WithCard()
    {
        var card = await IssueCardAsync(type: CardType.Credit, limit: 3000m);

        card.AccountId.Should().Be(FakeAccountServiceClient.AccountId);
        card.CustomerId.Should().Be(FakeAccountServiceClient.CustomerId);
        card.CardHolderName.Should().Be("JANE DOE"); // from FakeCustomerServiceClient
        card.Type.Should().Be(CardType.Credit);
        card.Status.Should().Be(CardStatus.Active);
        card.SpendingLimit.Should().Be(3000m);
        card.MaskedCardNumber.Should().MatchRegex(@"^\*{4} \*{4} \*{4} \d{4}$");
        card.ExpiryDate.Should().BeAfter(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3)));
    }

    [Fact]
    public async Task IssueCard_NoToken_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var body = new IssueCardRequest(FakeAccountServiceClient.AccountId, CardType.Debit, 500m);

        var response = await anonClient.PostAsJsonAsync("/api/cards", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IssueCard_UnknownAccount_Returns404()
    {
        var body = new IssueCardRequest(Guid.NewGuid(), CardType.Debit, 500m);

        var response = await _client.PostAsJsonAsync("/api/cards", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IssueCard_NegativeSpendingLimit_Returns400()
    {
        var body = new IssueCardRequest(FakeAccountServiceClient.AccountId, CardType.Debit, -100m);

        var response = await _client.PostAsJsonAsync("/api/cards", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueCard_SpendingLimitOverMax_Returns400()
    {
        var body = new IssueCardRequest(FakeAccountServiceClient.AccountId, CardType.Debit, 200_000m);

        var response = await _client.PostAsJsonAsync("/api/cards", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingCard_Returns200()
    {
        var issued = await IssueCardAsync();

        var response = await _client.GetAsync($"/api/cards/{issued.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        card!.Id.Should().Be(issued.Id);
        card.MaskedCardNumber.Should().Be(issued.MaskedCardNumber);
    }

    [Fact]
    public async Task GetById_UnknownCard_Returns404()
    {
        var response = await _client.GetAsync($"/api/cards/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NoToken_Returns401()
    {
        var issued = await IssueCardAsync();
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/cards/{issued.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Get by account

    [Fact]
    public async Task GetByAccount_ReturnsCardsForAccount()
    {
        var accountId = FakeAccountServiceClient.AccountId;
        await IssueCardAsync();
        await IssueCardAsync();

        var response = await _client.GetAsync($"/api/cards/account/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cards = await response.Content.ReadFromJsonAsync<List<CardResponse>>(JsonOptions);
        cards.Should().NotBeEmpty();
        cards!.Should().AllSatisfy(c => c.AccountId.Should().Be(accountId));
    }

    [Fact]
    public async Task GetByAccount_UnknownAccount_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/cards/account/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cards = await response.Content.ReadFromJsonAsync<List<CardResponse>>(JsonOptions);
        cards.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAccount_NoToken_Returns401()
    {
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/cards/account/{FakeAccountServiceClient.AccountId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Freeze

    [Fact]
    public async Task Freeze_ActiveCard_Returns200WithFrozenStatus()
    {
        var issued = await IssueCardAsync();

        var response = await _client.PostAsync($"/api/cards/{issued.Id}/freeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        card!.Status.Should().Be(CardStatus.Frozen);
        card.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Freeze_AlreadyFrozenCard_Returns400()
    {
        var seeded = await SeedCardAsync(CardStatus.Frozen);

        var response = await _client.PostAsync($"/api/cards/{seeded.Id}/freeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Freeze_UnknownCard_Returns404()
    {
        var response = await _client.PostAsync($"/api/cards/{Guid.NewGuid()}/freeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Freeze_NoToken_Returns401()
    {
        var issued = await IssueCardAsync();
        var anonClient = _factory.CreateClient();

        var response = await anonClient.PostAsync($"/api/cards/{issued.Id}/freeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Unfreeze

    [Fact]
    public async Task Unfreeze_FrozenCard_Returns200WithActiveStatus()
    {
        var seeded = await SeedCardAsync(CardStatus.Frozen);

        var response = await _client.PostAsync($"/api/cards/{seeded.Id}/unfreeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        card!.Status.Should().Be(CardStatus.Active);
    }

    [Fact]
    public async Task Unfreeze_ActiveCard_Returns400()
    {
        var issued = await IssueCardAsync();

        var response = await _client.PostAsync($"/api/cards/{issued.Id}/unfreeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unfreeze_UnknownCard_Returns404()
    {
        var response = await _client.PostAsync($"/api/cards/{Guid.NewGuid()}/unfreeze", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Update spending limit

    [Fact]
    public async Task UpdateSpendingLimit_ValidAmount_Returns200WithNewLimit()
    {
        var issued = await IssueCardAsync(limit: 1000m);

        var body = new UpdateSpendingLimitRequest(5000m);
        var response = await _client.PutAsJsonAsync($"/api/cards/{issued.Id}/spendingLimit", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        card!.SpendingLimit.Should().Be(5000m);
        card.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSpendingLimit_CancelledCard_Returns400()
    {
        var seeded = await SeedCardAsync(CardStatus.Cancelled);

        var body = new UpdateSpendingLimitRequest(2000m);
        var response = await _client.PutAsJsonAsync($"/api/cards/{seeded.Id}/spendingLimit", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSpendingLimit_NegativeAmount_Returns400()
    {
        var issued = await IssueCardAsync();

        var body = new UpdateSpendingLimitRequest(-500m);
        var response = await _client.PutAsJsonAsync($"/api/cards/{issued.Id}/spendingLimit", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSpendingLimit_UnknownCard_Returns404()
    {
        var body = new UpdateSpendingLimitRequest(2000m);
        var response = await _client.PutAsJsonAsync($"/api/cards/{Guid.NewGuid()}/spendingLimit", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSpendingLimit_NoToken_Returns401()
    {
        var issued = await IssueCardAsync();
        var anonClient = _factory.CreateClient();

        var body = new UpdateSpendingLimitRequest(2000m);
        var response = await anonClient.PutAsJsonAsync($"/api/cards/{issued.Id}/spendingLimit", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Full cycle

    [Fact]
    public async Task FullLifecycle_IssueFreezeThenUnfreezeThenUpdateLimit()
    {
        // Issue
        var card = await IssueCardAsync(limit: 1000m);
        card.Status.Should().Be(CardStatus.Active);

        // Freeze
        var frozen = await (await _client.PostAsync($"/api/cards/{card.Id}/freeze", null))
            .Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        frozen!.Status.Should().Be(CardStatus.Frozen);

        // Unfreeze
        var unfrozen = await (await _client.PostAsync($"/api/cards/{card.Id}/unfreeze", null))
            .Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        unfrozen!.Status.Should().Be(CardStatus.Active);

        // Update limit
        var updated = await (await _client.PutAsJsonAsync(
            $"/api/cards/{card.Id}/spendingLimit",
            new UpdateSpendingLimitRequest(9999m),
            JsonOptions)).Content.ReadFromJsonAsync<CardResponse>(JsonOptions);
        updated!.SpendingLimit.Should().Be(9999m);
    }
}
