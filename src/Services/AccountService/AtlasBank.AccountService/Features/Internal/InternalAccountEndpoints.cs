using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.AccountService.Features.Internal;

public static class InternalAccountEndpoints
{
    // Account.Balance is guarded by an optimistic concurrency token (see
    // AccountDbContext.UseXminAsConcurrencyToken) rather than a database or distributed
    // lock — there's a single Postgres instance here, so a plain concurrency check gives
    // the same correctness guarantee without extra infrastructure. This is the retry
    // side of that: on a lost race, reload the row's current state and reapply the same
    // mutation, rather than surfacing the conflict as a failure to the caller.
    private const int MaxConcurrencyRetries = 5;

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

    /// <summary>
    /// Applies <paramref name="mutate"/> (a Credit or Debit) and saves, retrying against
    /// the row's latest state if another request updated it first. <paramref name="mutate"/>
    /// re-runs on every attempt so it's re-validated (e.g. insufficient-funds) against
    /// fresh data each time, not just retried blindly.
    /// </summary>
    private static async Task<IResult> ApplyWithRetryAsync(
        Account account,
        Action<Account> mutate,
        IAccountRepository repo,
        CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                mutate(account);
                await repo.SaveChangesAsync(ct);
                return Results.Ok(new { account.Balance });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // Small jittered backoff spreads retries out under heavy contention
                // instead of every loser immediately re-colliding with the same
                // competitors on the next attempt.
                await Task.Delay(Random.Shared.Next(10, 40) * attempt, ct);
                await repo.ReloadAsync(account, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Retries exhausted under sustained contention on this one row. Fail
                // loudly and explicitly (409, safe to retry) instead of letting this
                // surface as an opaque 500 — no data was lost, this request's change
                // was simply never applied.
                return Results.Conflict($"Too many concurrent updates to account {account.Id}. Please retry.");
            }
        }
    }
}

public record BalanceChangeRequest(decimal Amount);
