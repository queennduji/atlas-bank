using AtlasBank.StatementService.Data.Repositories;
using AtlasBank.StatementService.Domain.Entities;
using AtlasBank.StatementService.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AtlasBank.StatementService.Features.Statements;

public static class StatementEndpoints
{
    public static void MapStatementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/statements").RequireAuthorization();

        group.MapPost("/generate", GenerateStatement);
        group.MapGet("/{id:guid}", GetStatement);
        group.MapGet("/account/{accountId:guid}", GetByAccount);
    }

    private static async Task<IResult> GenerateStatement(
        [FromBody] GenerateStatementRequest request,
        IValidator<GenerateStatementRequest> validator,
        IStatementRepository repo,
        IAccountServiceClient accountClient,
        ICustomerServiceClient customerClient,
        ITransactionServiceClient transactionClient,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var account = await accountClient.GetByIdAsync(request.AccountId, ct);
        if (account is null)
            return Results.NotFound(new { message = "Account not found." });

        var keycloakId = user.FindFirst("sub")?.Value;
        var customer = await customerClient.GetByIdAsync(account.CustomerId, ct);
        if (customer is null)
            return Results.NotFound(new { message = "Customer not found." });

        var transactions = await transactionClient.GetByAccountAsync(
            request.AccountId, request.PeriodStart, request.PeriodEnd, ct);

        var txTuples = transactions.Select(t =>
        {
            // Resolve transfer direction relative to this account
            var type = t.Type == "Transfer"
                ? (t.ToAccountId == request.AccountId ? "TransferIn" : "TransferOut")
                : t.Type;
            return (t.Id, t.CreatedAt, t.Reference, t.Description, type, t.Amount);
        });

        var statement = Statement.Generate(
            account.Id, customer.Id,
            account.AccountNumber,
            $"{customer.FirstName} {customer.LastName}".ToUpperInvariant(),
            account.Currency,
            request.PeriodStart, request.PeriodEnd,
            openingBalance: 0, // opening balance calculation requires full history
            txTuples);

        await repo.AddAsync(statement, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Created($"/api/statements/{statement.Id}", ToResponse(statement));
    }

    private static async Task<IResult> GetStatement(
        Guid id, IStatementRepository repo, CancellationToken ct)
    {
        var statement = await repo.GetByIdAsync(id, ct);
        return statement is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(statement));
    }

    private static async Task<IResult> GetByAccount(
        Guid accountId, IStatementRepository repo, CancellationToken ct)
    {
        var statements = await repo.GetByAccountIdAsync(accountId, ct);
        var summaries = statements.Select(s => new StatementSummaryResponse(
            s.Id, s.AccountId, s.AccountNumber,
            s.PeriodStart, s.PeriodEnd, s.ClosingBalance, s.GeneratedAt));
        return Results.Ok(summaries);
    }

    private static StatementResponse ToResponse(Statement s) => new(
        s.Id, s.AccountId, s.CustomerId,
        s.AccountNumber, s.CustomerName, s.Currency,
        s.PeriodStart, s.PeriodEnd,
        s.OpeningBalance, s.ClosingBalance,
        s.TotalCredits, s.TotalDebits,
        s.GeneratedAt,
        s.Lines.Select(l => new StatementLineResponse(
            l.TransactionId, l.Date, l.Reference,
            l.Description, l.Type, l.Amount, l.RunningBalance)).ToList());
}
