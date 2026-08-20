using AtlasBank.LedgerService.Data.Repositories;
using AtlasBank.LedgerService.Domain.Entities;

namespace AtlasBank.LedgerService.Features.Ledger;

public static class LedgerEndpoints
{
    public static void MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ledger").RequireAuthorization();

        group.MapGet("/account/{accountId:guid}", GetByAccount);
        group.MapGet("/account/{accountId:guid}/balance", GetBalance);
    }

    private static async Task<IResult> GetByAccount(Guid accountId, ILedgerRepository repo, CancellationToken ct)
    {
        var entries = await repo.GetByAccountIdAsync(accountId, ct);
        return Results.Ok(entries.Select(MapToResponse));
    }

    private static async Task<IResult> GetBalance(Guid accountId, ILedgerRepository repo, CancellationToken ct)
    {
        var balance = await repo.GetBalanceAsync(accountId, ct);
        return Results.Ok(new LedgerBalanceResponse(accountId, balance));
    }

    private static LedgerEntryResponse MapToResponse(LedgerEntry e) => new(
        e.Id, e.TransactionId, e.AccountId, e.Type, e.Amount, e.Currency,
        e.Reference, e.TransactionType, e.PostedAt, e.CreatedAt);
}
