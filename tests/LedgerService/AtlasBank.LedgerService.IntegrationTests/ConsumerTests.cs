using AtlasBank.LedgerService.Data;
using AtlasBank.LedgerService.Data.Repositories;
using AtlasBank.LedgerService.Domain.Enums;
using AtlasBank.LedgerService.Messaging.Consumers;
using AtlasBank.Shared.Messaging.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AtlasBank.LedgerService.IntegrationTests;

public class ConsumerTests : IAsyncLifetime
{
    private LedgerDbContext _db = default!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new LedgerDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    private TransactionCompletedConsumer BuildConsumer() =>
        new(new LedgerRepository(_db), NullLogger<TransactionCompletedConsumer>.Instance);

    private static Mock<ConsumeContext<TransactionCompletedEvent>> BuildContext(TransactionCompletedEvent evt)
    {
        var mock = new Mock<ConsumeContext<TransactionCompletedEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    [Fact]
    public async Task Deposit_PostsOneCreditEntry()
    {
        var accountId = Guid.NewGuid();
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), accountId, null, "Deposit", 500m, "USD", "REF001", DateTimeOffset.UtcNow);

        await BuildConsumer().Consume(BuildContext(evt).Object);

        var entries = await _db.LedgerEntries.Where(e => e.AccountId == accountId).ToListAsync();
        entries.Should().ContainSingle();
        entries[0].Type.Should().Be(LedgerEntryType.Credit);
        entries[0].Amount.Should().Be(500m);
        entries[0].TransactionId.Should().Be(evt.TransactionId);
    }

    [Fact]
    public async Task Withdrawal_PostsOneDebitEntry()
    {
        var accountId = Guid.NewGuid();
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), accountId, null, "Withdrawal", 200m, "USD", "REF002", DateTimeOffset.UtcNow);

        await BuildConsumer().Consume(BuildContext(evt).Object);

        var entries = await _db.LedgerEntries.Where(e => e.AccountId == accountId).ToListAsync();
        entries.Should().ContainSingle();
        entries[0].Type.Should().Be(LedgerEntryType.Debit);
    }

    [Fact]
    public async Task Transfer_PostsDebitOnSourceAndCreditOnDestination()
    {
        var fromAccount = Guid.NewGuid();
        var toAccount = Guid.NewGuid();
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), fromAccount, toAccount, "Transfer", 100m, "USD", "REF003", DateTimeOffset.UtcNow);

        await BuildConsumer().Consume(BuildContext(evt).Object);

        var entries = await _db.LedgerEntries.Where(e => e.TransactionId == evt.TransactionId).ToListAsync();
        entries.Should().HaveCount(2);
        entries.Should().ContainSingle(e => e.AccountId == fromAccount && e.Type == LedgerEntryType.Debit);
        entries.Should().ContainSingle(e => e.AccountId == toAccount && e.Type == LedgerEntryType.Credit);
    }

    [Fact]
    public async Task Transfer_MissingToAccountId_PostsDebitSideOnly()
    {
        var fromAccount = Guid.NewGuid();
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), fromAccount, null, "Transfer", 100m, "USD", "REF004", DateTimeOffset.UtcNow);

        await BuildConsumer().Consume(BuildContext(evt).Object);

        var entries = await _db.LedgerEntries.Where(e => e.TransactionId == evt.TransactionId).ToListAsync();
        entries.Should().ContainSingle();
        entries[0].Type.Should().Be(LedgerEntryType.Debit);
    }

    [Fact]
    public async Task RedeliveredEvent_DoesNotDoublePost()
    {
        var accountId = Guid.NewGuid();
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), accountId, null, "Deposit", 500m, "USD", "REF005", DateTimeOffset.UtcNow);

        var consumer = BuildConsumer();
        await consumer.Consume(BuildContext(evt).Object);
        await consumer.Consume(BuildContext(evt).Object); // simulate MassTransit redelivery

        var entries = await _db.LedgerEntries.Where(e => e.TransactionId == evt.TransactionId).ToListAsync();
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task UnrecognizedTransactionType_PostsNothing()
    {
        var accountId = Guid.NewGuid();
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), accountId, null, "Fee", 10m, "USD", "REF006", DateTimeOffset.UtcNow);

        await BuildConsumer().Consume(BuildContext(evt).Object);

        var entries = await _db.LedgerEntries.Where(e => e.AccountId == accountId).ToListAsync();
        entries.Should().BeEmpty();
    }
}
