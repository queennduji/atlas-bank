using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Domain.Entities;
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
        HttpContext http,
        CancellationToken ct)
    {
        var ownerId = http.User.FindFirst("sub")?.Value;
        if (ownerId is null) return Results.Unauthorized();

        var account = Account.Create(ownerId, request.Type, request.Currency);
        await repo.AddAsync(account, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Created($"/api/accounts/{account.Id}", ToResponse(account));
    }

    private static async Task<IResult> GetById(
        Guid id,
        IAccountRepository repo,
        HttpContext http,
        CancellationToken ct)
    {
        var account = await repo.GetByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        var ownerId = http.User.FindFirst("sub")?.Value;
        if (account.OwnerId != ownerId) return Results.Forbid();

        return Results.Ok(ToResponse(account));
    }

    private static async Task<IResult> GetMyAccounts(
        IAccountRepository repo,
        HttpContext http,
        CancellationToken ct)
    {
        var ownerId = http.User.FindFirst("sub")?.Value;
        if (ownerId is null) return Results.Unauthorized();

        var accounts = await repo.GetByOwnerIdAsync(ownerId, ct);
        return Results.Ok(accounts.Select(ToResponse));
    }

    private static AccountResponse ToResponse(Domain.Entities.Account a) =>
        new(a.Id, a.OwnerId, a.AccountNumber, a.Type, a.Status, a.Balance, a.Currency, a.CreatedAt);
}
