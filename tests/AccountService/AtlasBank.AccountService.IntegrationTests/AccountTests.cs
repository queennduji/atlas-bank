using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AtlasBank.AccountService.Features.Accounts;
using AtlasBank.AccountService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AtlasBank.AccountService.IntegrationTests;

public class AccountTests(AccountServiceFactory factory) : IClassFixture<AccountServiceFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string _keycloakUserId = factory.DefaultKeycloakUserId;

    private HttpClient AuthenticatedClient()
    {
        var token = TestJwtTokenGenerator.GenerateToken(_keycloakUserId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    [Fact]
    public async Task CreateAccount_WithValidRequest_Returns201()
    {
        var client = AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(
            Domain.Enums.AccountType.Checking, "USD"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.CustomerId.Should().Be(factory.DefaultCustomerId);
        account.Type.Should().Be(Domain.Enums.AccountType.Checking);
        account.Balance.Should().Be(0);
        account.Currency.Should().Be("USD");
        account.AccountNumber.Should().StartWith("ATL");
    }

    [Fact]
    public async Task CreateAccount_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest(Domain.Enums.AccountType.Checking, "USD"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAccount_WithInvalidCurrency_Returns400()
    {
        var client = AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest(Domain.Enums.AccountType.Checking, "XYZ"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Currency");
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsAccount()
    {
        var client = AuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/accounts",
            new CreateAccountRequest(Domain.Enums.AccountType.Savings, "USD"));
        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>();

        var response = await client.GetAsync($"/api/accounts/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyAccounts_ReturnsAllAccountsForCustomer()
    {
        var client = AuthenticatedClient();

        await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(Domain.Enums.AccountType.Checking, "USD"));
        await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(Domain.Enums.AccountType.Savings, "USD"));

        var response = await client.GetAsync("/api/accounts/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponse>>();
        accounts.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
