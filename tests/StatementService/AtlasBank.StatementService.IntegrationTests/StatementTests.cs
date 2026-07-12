using AtlasBank.StatementService.Features.Statements;
using AtlasBank.StatementService.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtlasBank.StatementService.IntegrationTests;

public class StatementTests : IClassFixture<StatementServiceFactory>
{
    private readonly HttpClient _client;
    private readonly StatementServiceFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Guid KnownAccountId = FakeAccountServiceClient.AccountId;

    public StatementTests(StatementServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        var token = TestJwtTokenGenerator.GenerateToken(Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<StatementResponse> GenerateAsync(
        Guid? accountId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null)
    {
        var body = new GenerateStatementRequest(
            accountId ?? KnownAccountId,
            start ?? DateTimeOffset.UtcNow.AddDays(-30),
            end ?? DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/statements/generate", body, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<StatementResponse>(JsonOptions))!;
    }

    // POST /generate

    [Fact]
    public async Task Generate_NoTransactions_Returns201WithZeroBalances()
    {
        _factory.FakeTransactionClient.Clear();

        var stmt = await GenerateAsync();

        stmt.AccountId.Should().Be(KnownAccountId);
        stmt.AccountNumber.Should().Be("ATL0000001");
        stmt.CustomerName.Should().Be("JANE DOE");
        stmt.Currency.Should().Be("USD");
        stmt.OpeningBalance.Should().Be(0);
        stmt.ClosingBalance.Should().Be(0);
        stmt.TotalCredits.Should().Be(0);
        stmt.TotalDebits.Should().Be(0);
        stmt.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Generate_WithDepositsAndWithdrawal_CorrectRunningBalance()
    {
        _factory.FakeTransactionClient.Clear();
        var baseTime = DateTimeOffset.UtcNow.AddDays(-10);
        _factory.FakeTransactionClient.Seed([
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 1000m, "Salary", baseTime),
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 500m, "Bonus", baseTime.AddHours(1)),
            FakeTransactionServiceClient.MakeWithdrawal(KnownAccountId, 200m, "Rent", baseTime.AddHours(2))
        ]);

        var stmt = await GenerateAsync();

        stmt.TotalCredits.Should().Be(1500m);
        stmt.TotalDebits.Should().Be(200m);
        stmt.ClosingBalance.Should().Be(1300m);
        stmt.Lines.Should().HaveCount(3);

        var lines = stmt.Lines.ToList();
        lines[0].Type.Should().Be("Deposit");
        lines[0].Amount.Should().Be(1000m);
        lines[0].RunningBalance.Should().Be(1000m);

        lines[1].Type.Should().Be("Deposit");
        lines[1].Amount.Should().Be(500m);
        lines[1].RunningBalance.Should().Be(1500m);

        lines[2].Type.Should().Be("Withdrawal");
        lines[2].Amount.Should().Be(200m);
        lines[2].RunningBalance.Should().Be(1300m);
    }

    [Fact]
    public async Task Generate_WithTransfers_CorrectDirectionAndBalance()
    {
        _factory.FakeTransactionClient.Clear();
        var otherAccountId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddDays(-5);

        _factory.FakeTransactionClient.Seed([
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 2000m, "Opening", baseTime),
            // outgoing transfer (KnownAccount is sender)
            FakeTransactionServiceClient.MakeTransferOut(KnownAccountId, otherAccountId, 300m, baseTime.AddHours(1)),
            // incoming transfer (KnownAccount is recipient)
            FakeTransactionServiceClient.MakeTransferIn(otherAccountId, KnownAccountId, 100m, baseTime.AddHours(2))
        ]);

        var stmt = await GenerateAsync();

        stmt.TotalCredits.Should().Be(2100m);  // 2000 + 100 incoming
        stmt.TotalDebits.Should().Be(300m);    // 300 outgoing
        stmt.ClosingBalance.Should().Be(1800m); // 2000 - 300 + 100

        var lines = stmt.Lines.ToList();
        lines[1].Type.Should().Be("TransferOut");
        lines[1].Amount.Should().Be(300m);
        lines[1].RunningBalance.Should().Be(1700m);

        lines[2].Type.Should().Be("TransferIn");
        lines[2].Amount.Should().Be(100m);
        lines[2].RunningBalance.Should().Be(1800m);
    }

    [Fact]
    public async Task Generate_NoToken_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var body = new GenerateStatementRequest(KnownAccountId,
            DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);

        var response = await anonClient.PostAsJsonAsync("/api/statements/generate", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_UnknownAccount_Returns404()
    {
        var body = new GenerateStatementRequest(Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/statements/generate", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Generate_PeriodEndBeforeStart_Returns400()
    {
        var body = new GenerateStatementRequest(KnownAccountId,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1));

        var response = await _client.PostAsJsonAsync("/api/statements/generate", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Generate_PeriodExceedsOneYear_Returns400()
    {
        var body = new GenerateStatementRequest(KnownAccountId,
            DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/statements/generate", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Generate_EmptyBody_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/statements/generate",
            new { }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("AccountId");
    }

    [Fact]
    public async Task Generate_TransactionsOutsidePeriod_NotIncluded()
    {
        _factory.FakeTransactionClient.Clear();
        var inside = DateTimeOffset.UtcNow.AddDays(-5);
        var outside = DateTimeOffset.UtcNow.AddDays(-40);

        _factory.FakeTransactionClient.Seed([
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 1000m, "Inside period", inside),
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 999m, "Outside period", outside)
        ]);

        var stmt = await GenerateAsync(
            start: DateTimeOffset.UtcNow.AddDays(-30),
            end: DateTimeOffset.UtcNow);

        stmt.Lines.Should().HaveCount(1);
        stmt.TotalCredits.Should().Be(1000m);
    }

    // GET /{id}

    [Fact]
    public async Task GetById_ExistingStatement_Returns200WithLines()
    {
        _factory.FakeTransactionClient.Clear();
        _factory.FakeTransactionClient.Seed([
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 500m, "Test deposit")
        ]);

        var generated = await GenerateAsync();

        var response = await _client.GetAsync($"/api/statements/{generated.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stmt = await response.Content.ReadFromJsonAsync<StatementResponse>(JsonOptions);
        stmt!.Id.Should().Be(generated.Id);
        stmt.Lines.Should().HaveCount(1);
        stmt.Lines[0].Description.Should().Be("Test deposit");
    }

    [Fact]
    public async Task GetById_UnknownStatement_Returns404()
    {
        var response = await _client.GetAsync($"/api/statements/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NoToken_Returns401()
    {
        var generated = await GenerateAsync();
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/statements/{generated.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // GET /account/{accountId}

    [Fact]
    public async Task GetByAccount_NoStatements_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/statements/account/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<StatementSummaryResponse>>(JsonOptions);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAccount_MultipleStatements_ReturnsAll()
    {
        _factory.FakeTransactionClient.Clear();
        await GenerateAsync(start: DateTimeOffset.UtcNow.AddDays(-30), end: DateTimeOffset.UtcNow.AddDays(-15));
        await GenerateAsync(start: DateTimeOffset.UtcNow.AddDays(-14), end: DateTimeOffset.UtcNow);

        var response = await _client.GetAsync($"/api/statements/account/{KnownAccountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<StatementSummaryResponse>>(JsonOptions);
        list.Should().HaveCountGreaterThanOrEqualTo(2);
        list!.Should().AllSatisfy(s => s.AccountId.Should().Be(KnownAccountId));
    }

    [Fact]
    public async Task GetByAccount_NoToken_Returns401()
    {
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/statements/account/{KnownAccountId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Full lifecycle

    [Fact]
    public async Task FullLifecycle_GenerateThenFetchById()
    {
        _factory.FakeTransactionClient.Clear();
        var baseTime = DateTimeOffset.UtcNow.AddDays(-3);
        _factory.FakeTransactionClient.Seed([
            FakeTransactionServiceClient.MakeDeposit(KnownAccountId, 800m, "Direct deposit", baseTime),
            FakeTransactionServiceClient.MakeWithdrawal(KnownAccountId, 50m, "Coffee", baseTime.AddHours(1))
        ]);

        // Generate
        var stmt = await GenerateAsync();
        stmt.ClosingBalance.Should().Be(750m);
        stmt.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        // Fetch by ID — lines are persisted and returned
        var fetched = await (await _client.GetAsync($"/api/statements/{stmt.Id}"))
            .Content.ReadFromJsonAsync<StatementResponse>(JsonOptions);
        fetched!.Id.Should().Be(stmt.Id);
        fetched.ClosingBalance.Should().Be(750m);
        fetched.Lines.Should().HaveCount(2);

        // Appears in account list
        var list = await (await _client.GetAsync($"/api/statements/account/{KnownAccountId}"))
            .Content.ReadFromJsonAsync<List<StatementSummaryResponse>>(JsonOptions);
        list.Should().Contain(s => s.Id == stmt.Id);
        list!.First(s => s.Id == stmt.Id).ClosingBalance.Should().Be(750m);
    }
}
