using AtlasBank.TransactionService.Data.Repositories;
using AtlasBank.TransactionService.Domain.Entities;
using AtlasBank.TransactionService.Infrastructure;
using AtlasBank.TransactionService.Messaging.Events;
using FluentValidation;
using MassTransit;

namespace AtlasBank.TransactionService.Features.Transactions;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").RequireAuthorization();

        group.MapPost("/deposit", Deposit);
        group.MapPost("/withdraw", Withdraw);
        group.MapPost("/transfer", Transfer);
        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/account/{accountId:guid}", GetByAccount);
    }

    private static async Task<IResult> Deposit(
        DepositRequest request,
        IValidator<DepositRequest> validator,
        ITransactionRepository repo,
        IAccountServiceClient accountClient,
        IPublishEndpoint publisher,
        CancellationToken ct)
    {
        var validationError = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationError is not null) return validationError;

        var account = await accountClient.GetByIdAsync(request.AccountId, ct);
        if (account is null) return Results.NotFound("Account not found.");
        if (account.Status != 0) return Results.BadRequest("Account is not active.");

        var transaction = Transaction.CreateDeposit(request.AccountId, request.Amount, request.Currency, request.Description);
        await repo.AddAsync(transaction, ct);
        await repo.SaveChangesAsync(ct);

        var credited = await accountClient.CreditAsync(request.AccountId, request.Amount, ct);
        if (!credited)
        {
            transaction.Fail("Failed to credit account.");
            await repo.SaveChangesAsync(ct);
            return Results.BadRequest("Failed to process deposit.");
        }

        transaction.Complete();
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new TransactionCompletedEvent(
            transaction.Id, transaction.AccountId, transaction.ToAccountId,
            transaction.Type.ToString(), transaction.Amount, transaction.Currency,
            transaction.Reference, transaction.CompletedAt!.Value), ct);

        return Results.Created($"/api/transactions/{transaction.Id}", MapToResponse(transaction));
    }

    private static async Task<IResult> Withdraw(
        WithdrawRequest request,
        IValidator<WithdrawRequest> validator,
        ITransactionRepository repo,
        IAccountServiceClient accountClient,
        IPublishEndpoint publisher,
        CancellationToken ct)
    {
        var validationError = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationError is not null) return validationError;

        var account = await accountClient.GetByIdAsync(request.AccountId, ct);
        if (account is null) return Results.NotFound("Account not found.");
        if (account.Status != 0) return Results.BadRequest("Account is not active.");
        if (account.Balance < request.Amount) return Results.BadRequest("Insufficient funds.");

        var transaction = Transaction.CreateWithdrawal(request.AccountId, request.Amount, request.Currency, request.Description);
        await repo.AddAsync(transaction, ct);
        await repo.SaveChangesAsync(ct);

        var debited = await accountClient.DebitAsync(request.AccountId, request.Amount, ct);
        if (!debited)
        {
            transaction.Fail("Failed to debit account.");
            await repo.SaveChangesAsync(ct);
            return Results.BadRequest("Failed to process withdrawal.");
        }

        transaction.Complete();
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new TransactionCompletedEvent(
            transaction.Id, transaction.AccountId, transaction.ToAccountId,
            transaction.Type.ToString(), transaction.Amount, transaction.Currency,
            transaction.Reference, transaction.CompletedAt!.Value), ct);

        return Results.Created($"/api/transactions/{transaction.Id}", MapToResponse(transaction));
    }

    private static async Task<IResult> Transfer(
        TransferRequest request,
        IValidator<TransferRequest> validator,
        ITransactionRepository repo,
        IAccountServiceClient accountClient,
        IPublishEndpoint publisher,
        CancellationToken ct)
    {
        var validationError = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationError is not null) return validationError;

        var fromAccount = await accountClient.GetByIdAsync(request.FromAccountId, ct);
        if (fromAccount is null) return Results.NotFound("Source account not found.");
        if (fromAccount.Status != 0) return Results.BadRequest("Source account is not active.");
        if (fromAccount.Balance < request.Amount) return Results.BadRequest("Insufficient funds.");

        var toAccount = await accountClient.GetByIdAsync(request.ToAccountId, ct);
        if (toAccount is null) return Results.NotFound("Destination account not found.");
        if (toAccount.Status != 0) return Results.BadRequest("Destination account is not active.");

        var transaction = Transaction.CreateTransfer(request.FromAccountId, request.ToAccountId, request.Amount, request.Currency, request.Description);
        await repo.AddAsync(transaction, ct);
        await repo.SaveChangesAsync(ct);

        var debited = await accountClient.DebitAsync(request.FromAccountId, request.Amount, ct);
        if (!debited)
        {
            transaction.Fail("Failed to debit source account.");
            await repo.SaveChangesAsync(ct);
            return Results.BadRequest("Failed to process transfer.");
        }

        var credited = await accountClient.CreditAsync(request.ToAccountId, request.Amount, ct);
        if (!credited)
        {
            // Attempt to reverse the debit
            await accountClient.CreditAsync(request.FromAccountId, request.Amount, ct);
            transaction.Fail("Failed to credit destination account. Debit reversed.");
            await repo.SaveChangesAsync(ct);
            return Results.BadRequest("Failed to complete transfer.");
        }

        transaction.Complete();
        await repo.SaveChangesAsync(ct);

        await publisher.Publish(new TransactionCompletedEvent(
            transaction.Id, transaction.AccountId, transaction.ToAccountId,
            transaction.Type.ToString(), transaction.Amount, transaction.Currency,
            transaction.Reference, transaction.CompletedAt!.Value), ct);

        return Results.Created($"/api/transactions/{transaction.Id}", MapToResponse(transaction));
    }

    private static async Task<IResult> GetById(Guid id, ITransactionRepository repo, CancellationToken ct)
    {
        var transaction = await repo.GetByIdAsync(id, ct);
        if (transaction is null) return Results.NotFound();
        return Results.Ok(MapToResponse(transaction));
    }

    private static async Task<IResult> GetByAccount(Guid accountId, ITransactionRepository repo, CancellationToken ct)
    {
        var transactions = await repo.GetByAccountIdAsync(accountId, ct);
        return Results.Ok(transactions.Select(MapToResponse));
    }

    private static TransactionResponse MapToResponse(Transaction t) => new(
        t.Id, t.AccountId, t.ToAccountId, t.Type, t.Status,
        t.Amount, t.Currency, t.Reference, t.Description,
        t.FailureReason, t.CreatedAt, t.CompletedAt);
}
