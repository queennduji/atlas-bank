using AtlasBank.LedgerService.Data.Repositories;
using AtlasBank.LedgerService.Domain.Entities;
using AtlasBank.LedgerService.Domain.Enums;
using AtlasBank.Shared.Messaging.Events;
using MassTransit;

namespace AtlasBank.LedgerService.Messaging.Consumers;

public class TransactionCompletedConsumer(
    ILedgerRepository repo,
    ILogger<TransactionCompletedConsumer> logger) : IConsumer<TransactionCompletedEvent>
{
    public async Task Consume(ConsumeContext<TransactionCompletedEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        // RabbitMQ/MassTransit delivery is at-least-once — a redelivered message must not
        // double-post. One transaction always produces its first entry on evt.AccountId, so
        // checking for that is enough to detect a repeat.
        if (await repo.ExistsByTransactionIdAsync(evt.TransactionId, ct))
        {
            logger.LogInformation("Ledger entries for transaction {TransactionId} already exist, skipping", evt.TransactionId);
            return;
        }

        switch (evt.TransactionType)
        {
            case "Deposit":
                await repo.AddAsync(Post(evt, evt.AccountId, LedgerEntryType.Credit), ct);
                break;

            case "Withdrawal":
                await repo.AddAsync(Post(evt, evt.AccountId, LedgerEntryType.Debit), ct);
                break;

            case "Transfer":
                await repo.AddAsync(Post(evt, evt.AccountId, LedgerEntryType.Debit), ct);
                if (evt.ToAccountId is { } toAccountId)
                {
                    await repo.AddAsync(Post(evt, toAccountId, LedgerEntryType.Credit), ct);
                }
                else
                {
                    logger.LogWarning("Transfer {TransactionId} has no ToAccountId — posting the debit side only", evt.TransactionId);
                }
                break;

            default:
                logger.LogWarning("Unrecognized transaction type {Type} for transaction {TransactionId}", evt.TransactionType, evt.TransactionId);
                return;
        }

        await repo.SaveChangesAsync(ct);
    }

    private static LedgerEntry Post(TransactionCompletedEvent evt, Guid accountId, LedgerEntryType type) =>
        LedgerEntry.Create(evt.TransactionId, accountId, type, evt.Amount, evt.Currency, evt.Reference, evt.TransactionType, evt.CompletedAt);
}
