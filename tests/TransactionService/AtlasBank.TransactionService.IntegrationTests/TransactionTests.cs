using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AtlasBank.TransactionService.Domain.Enums;
using AtlasBank.TransactionService.Features.Transactions;
using AtlasBank.TransactionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AtlasBank.TransactionService.IntegrationTests;

public class TransactionTests : IClassFixture<TransactionServiceFactory>
{
    private readonly HttpClient _client;
    private readonly TransactionServiceFactory _factory;
    private readonly string _token;

    public TransactionTests(TransactionServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _token = TestJwtTokenGenerator.GenerateToken(Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    // ─── Deposit ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deposit_ValidRequest_Returns201WithCompletedTransaction()
    {
        var request = new DepositRequest(FakeAccountServiceClient.ActiveAccountId, 250m);

        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tx = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        tx.Should().NotBeNull();
        tx!.AccountId.Should().Be(FakeAccountServiceClient.ActiveAccountId);
        tx.Amount.Should().Be(250m);
        tx.Type.Should().Be(TransactionType.Deposit);
        tx.Status.Should().Be(TransactionStatus.Completed);
        tx.Reference.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Deposit_AccountNotFound_Returns404()
    {
        var request = new DepositRequest(Guid.NewGuid(), 100m);

        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deposit_FrozenAccount_Returns400()
    {
        var request = new DepositRequest(FakeAccountServiceClient.FrozenAccountId, 100m);

        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_ZeroAmount_Returns400()
    {
        var request = new DepositRequest(FakeAccountServiceClient.ActiveAccountId, 0m);

        var response = await _client.PostAsJsonAsync("/api/transactions/deposit", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_NoToken_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new DepositRequest(FakeAccountServiceClient.ActiveAccountId, 100m);

        var response = await client.PostAsJsonAsync("/api/transactions/deposit", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Withdraw ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Withdraw_ValidRequest_Returns201WithCompletedTransaction()
    {
        var request = new WithdrawRequest(FakeAccountServiceClient.ActiveAccountId, 100m);

        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tx = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        tx!.Type.Should().Be(TransactionType.Withdrawal);
        tx.Status.Should().Be(TransactionStatus.Completed);
        tx.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_Returns400()
    {
        var request = new WithdrawRequest(FakeAccountServiceClient.SecondAccountId, 9999m);

        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Withdraw_FrozenAccount_Returns400()
    {
        var request = new WithdrawRequest(FakeAccountServiceClient.FrozenAccountId, 50m);

        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Withdraw_AccountNotFound_Returns404()
    {
        var request = new WithdrawRequest(Guid.NewGuid(), 50m);

        var response = await _client.PostAsJsonAsync("/api/transactions/withdraw", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── Transfer ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Transfer_ValidRequest_Returns201WithCompletedTransaction()
    {
        var request = new TransferRequest(FakeAccountServiceClient.ActiveAccountId, FakeAccountServiceClient.SecondAccountId, 50m);

        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tx = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        tx!.Type.Should().Be(TransactionType.Transfer);
        tx.Status.Should().Be(TransactionStatus.Completed);
        tx.ToAccountId.Should().Be(FakeAccountServiceClient.SecondAccountId);
    }

    [Fact]
    public async Task Transfer_SourceAccountNotFound_Returns404()
    {
        var request = new TransferRequest(Guid.NewGuid(), FakeAccountServiceClient.SecondAccountId, 50m);

        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Transfer_DestinationAccountFrozen_Returns400()
    {
        var request = new TransferRequest(FakeAccountServiceClient.ActiveAccountId, FakeAccountServiceClient.FrozenAccountId, 50m);

        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_Returns400()
    {
        var request = new TransferRequest(FakeAccountServiceClient.SecondAccountId, FakeAccountServiceClient.ActiveAccountId, 99999m);

        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingTransaction_Returns200()
    {
        var depositRequest = new DepositRequest(FakeAccountServiceClient.ActiveAccountId, 10m);
        var createResponse = await _client.PostAsJsonAsync("/api/transactions/deposit", depositRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        var response = await _client.GetAsync($"/api/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tx = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        tx!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistentTransaction_Returns404()
    {
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── GetByAccount ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByAccount_ReturnsTransactionsForAccount()
    {
        var accountId = FakeAccountServiceClient.ActiveAccountId;
        await _client.PostAsJsonAsync("/api/transactions/deposit", new DepositRequest(accountId, 5m));
        await _client.PostAsJsonAsync("/api/transactions/deposit", new DepositRequest(accountId, 5m));

        var response = await _client.GetAsync($"/api/transactions/account/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        transactions.Should().NotBeEmpty();
        transactions!.Should().AllSatisfy(t => t.AccountId.Should().Be(accountId));
    }

    [Fact]
    public async Task GetByAccount_UnknownAccount_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/transactions/account/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        transactions.Should().BeEmpty();
    }
}
