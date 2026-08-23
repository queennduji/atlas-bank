using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBank.AccountService.Features.Internal;

public static class InternalAccountEndpoints
{
    public static void MapInternalAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/accounts");

        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/{id:guid}/credit", Credit);
        group.MapPost("/{id:guid}/debit", Debit);
    }

    private static async Task<IResult> GetById(Guid id, IAccountRepository repo, CancellationToken ct)
    {
        var account = await repo.GetByIdAsync(id, ct);
        if (account is null) return Results.NotFound();
        return Results.Ok(new { account.Id, account.CustomerId, account.AccountNumber, account.Status, account.Balance, account.Currency });
    }

    private static async Task<IResult> Credit(
        Guid id,
        [FromBody] BalanceChangeRequest request,
        IAccountRepository repo,
        CancellationToken ct)
    {
        var account = await repo.GetByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        return await ApplyWithRetryAsync(account, a => a.Credit(request.Amount), repo, ct);
    }

    private static async Task<IResult> Debit(
        Guid id,
        [FromBody] BalanceChangeRequest request,
        IAccountRepository repo,
        CancellationToken ct)
    {
        var account = await repo.GetByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        return await ApplyWithRetryAsync(account, a => a.Debit(request.Amount), repo, ct);
    }

    private static async Task<IResult> ApplyWithRetryAsync(
        Account account,
        Action<Account> mutate,
        IAccountRepository repo,
        CancellationToken ct)
    {
        var result = await AccountConcurrencyRetry.ApplyAsync(account, mutate, repo, ct);
        return result.Outcome switch
        {
            ConcurrencyOutcome.Success => Results.Ok(new { account.Balance }),
            ConcurrencyOutcome.BusinessRuleViolation => Results.BadRequest(result.Message),
            _ => Results.Conflict(result.Message),
        };
    }
}

public record BalanceChangeRequest(decimal Amount);
