using System.Net;
using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Models;
using AtlasBank.Clients.Core.Tests.TestSupport;
using FluentAssertions;

namespace AtlasBank.Clients.Core.Tests.Api;

public class AtlasApiClientTests
{
    private static (AtlasApiClient Client, FakeHttpMessageHandler Handler) CreateClient()
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://gateway.test") };
        return (new AtlasApiClient(http), handler);
    }

    [Fact]
    public async Task GetMyAccountsAsync_DeserializesCamelCaseJsonWithNumericEnums()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueJson(HttpStatusCode.OK, """
            [
              { "id": "11111111-1111-1111-1111-111111111111", "customerId": "22222222-2222-2222-2222-222222222222",
                "accountNumber": "ATL-0001", "type": 1, "status": 0, "balance": 250.75, "currency": "USD",
                "createdAt": "2026-01-01T00:00:00Z" }
            ]
            """);

        var accounts = await client.GetMyAccountsAsync();

        accounts.Should().ContainSingle();
        var account = accounts[0];
        account.AccountNumber.Should().Be("ATL-0001");
        account.Type.Should().Be(AccountType.Savings); // 1
        account.Status.Should().Be(AccountStatus.Active); // 0
        account.Balance.Should().Be(250.75m);
    }

    [Fact]
    public async Task DepositAsync_AttachesTheProvidedIdempotencyKey()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueJson(HttpStatusCode.OK, """
            { "id": "11111111-1111-1111-1111-111111111111", "accountId": "22222222-2222-2222-2222-222222222222",
              "toAccountId": null, "type": 0, "status": 1, "amount": 50, "currency": "USD",
              "reference": "TXN-1", "description": null, "failureReason": null, "createdAt": "2026-01-01T00:00:00Z",
              "completedAt": "2026-01-01T00:00:01Z" }
            """);

        await client.DepositAsync(
            new DepositRequest { AccountId = Guid.NewGuid(), Amount = 50 },
            idempotencyKey: "fixed-key-123");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Headers.GetValues("Idempotency-Key").Should().ContainSingle().Which.Should().Be("fixed-key-123");
    }

    [Fact]
    public async Task DepositAsync_GeneratesAFreshIdempotencyKey_WhenNoneIsProvided()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueJson(HttpStatusCode.OK, """
            { "id": "11111111-1111-1111-1111-111111111111", "accountId": "22222222-2222-2222-2222-222222222222",
              "toAccountId": null, "type": 0, "status": 1, "amount": 50, "currency": "USD",
              "reference": "TXN-1", "description": null, "failureReason": null, "createdAt": "2026-01-01T00:00:00Z",
              "completedAt": null }
            """);

        await client.DepositAsync(new DepositRequest { AccountId = Guid.NewGuid(), Amount = 50 });

        var key = handler.Requests[0].Headers.GetValues("Idempotency-Key").Single();
        Guid.TryParse(key, out _).Should().BeTrue();
    }

    [Fact]
    public async Task ThrowsApiException_WithTheFirstFieldError_ForAnAspNetValidationProblem()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueJson(HttpStatusCode.BadRequest, """
            { "title": "One or more validation errors occurred.",
              "errors": { "Amount": ["Amount must be greater than zero."] } }
            """);

        var act = () => client.DepositAsync(new DepositRequest { AccountId = Guid.NewGuid(), Amount = -1 });

        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.Message.Should().Be("Amount must be greater than zero.");
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ThrowsApiException_WithTheMessageField_WhenPresent()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueJson(HttpStatusCode.Conflict, """{ "message": "Insufficient funds." }""");

        var act = () => client.GetMyAccountsAsync();

        (await act.Should().ThrowAsync<ApiException>()).Which.Message.Should().Be("Insufficient funds.");
    }

    [Fact]
    public async Task ThrowsApiException_WithTheRawBody_WhenItIsAPlainJsonString()
    {
        var (client, handler) = CreateClient();
        handler.EnqueueJson(HttpStatusCode.InternalServerError, "\"Something went wrong upstream.\"");

        var act = () => client.GetMyAccountsAsync();

        (await act.Should().ThrowAsync<ApiException>()).Which.Message.Should().Be("Something went wrong upstream.");
    }

    [Fact]
    public async Task ThrowsApiException_WithAGenericMessage_WhenTheBodyIsEmpty()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var act = () => client.GetMyAccountsAsync();

        (await act.Should().ThrowAsync<ApiException>()).Which.Message.Should().Contain("503");
    }
}
