using AtlasBank.AccountService.Data.Repositories;
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

        try
        {
            account.Credit(request.Amount);
            await repo.SaveChangesAsync(ct);
            return Results.Ok(new { account.Balance });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> Debit(
        Guid id,
        [FromBody] BalanceChangeRequest request,
        IAccountRepository repo,
        CancellationToken ct)
    {
        var account = await repo.GetByIdAsync(id, ct);
        if (account is null) return Results.NotFound();

        try
        {
            account.Debit(request.Amount);
            await repo.SaveChangesAsync(ct);
            return Results.Ok(new { account.Balance });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}

public record BalanceChangeRequest(decimal Amount);
