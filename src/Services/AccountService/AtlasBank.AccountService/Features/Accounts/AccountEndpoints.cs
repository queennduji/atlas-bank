using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Domain.Entities;
using AtlasBank.AccountService.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBank.AccountService.Features.Accounts;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").RequireAuthorization();

        group.MapPost("/", CreateAccount);
        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/me", GetMyAccounts);
    }

    private static async Task<IResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        IAccountRepository repo,
        ICustomerServiceClient customerClient,
        HttpContext http,
        CancellationToken ct)
    {
        var keycloakUserId = http.User.FindFirst("sub")?.Value;
        if (keycloakUserId is null) return Results.Unauthorized();

        var customer = await customerClient.GetByKeycloakUserIdAsync(keycloakUserId, ct);
        if (customer is null)
            return Results.BadRequest("No customer profile found. Please register as a customer first.");

        var account = Account.Create(customer.Id, request.Type, request.Currency);
        await repo.AddAsync(account, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Created($"/api/accounts/{account.Id}", ToResponse(account));
    }

    private static async Task<IResult> GetById(
        Guid id,
        IAccountRepository repo,
        ICustomerServiceClient customerClient,
        HttpContext http,
        CancellationToken ct)
    {
        var account = await repo.GetByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        var keycloakUserId = http.User.FindFirst("sub")?.Value;
        var customer = await customerClient.GetByKeycloakUserIdAsync(keycloakUserId!, ct);
        if (customer is null || account.CustomerId != customer.Id) return Results.Forbid();

        return Results.Ok(ToResponse(account));
    }

    private static async Task<IResult> GetMyAccounts(
        IAccountRepository repo,
        ICustomerServiceClient customerClient,
        HttpContext http,
        CancellationToken ct)
    {
        var keycloakUserId = http.User.FindFirst("sub")?.Value;
        if (keycloakUserId is null) return Results.Unauthorized();

        var customer = await customerClient.GetByKeycloakUserIdAsync(keycloakUserId, ct);
        if (customer is null)
            return Results.BadRequest("No customer profile found. Please register as a customer first.");

        var accounts = await repo.GetByCustomerIdAsync(customer.Id, ct);
        return Results.Ok(accounts.Select(ToResponse));
    }

    private static AccountResponse ToResponse(Account a) =>
        new(a.Id, a.CustomerId, a.AccountNumber, a.Type, a.Status, a.Balance, a.Currency, a.CreatedAt);
}
