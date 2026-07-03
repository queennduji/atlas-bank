using AtlasBank.NotificationService.Data;
using AtlasBank.NotificationService.Data.Repositories;
using AtlasBank.NotificationService.Domain.Enums;
using AtlasBank.NotificationService.Messaging.Consumers;
using AtlasBank.NotificationService.IntegrationTests.Infrastructure;
using AtlasBank.Shared.Messaging.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AtlasBank.NotificationService.IntegrationTests;

public class ConsumerTests : IAsyncLifetime
{
    private NotificationDbContext _db = default!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new NotificationDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    private (TransactionCompletedConsumer Consumer, SpyEmailService Email, SpySmsService Sms, SpyPushNotificationService Push)
        BuildConsumer(bool includeDeviceToken = true)
    {
        var email = new SpyEmailService();
        var sms = new SpySmsService();
        var push = new SpyPushNotificationService();
        var accountClient = new FakeAccountServiceClient();
        var customerClient = new FakeCustomerServiceClient();
        var repo = new NotificationRepository(_db);

        var consumer = new TransactionCompletedConsumer(
            accountClient,
            customerClient,
            email,
            sms,
            push,
            repo,
            NullLogger<TransactionCompletedConsumer>.Instance);

        return (consumer, email, sms, push);
    }

    private static Mock<ConsumeContext<TransactionCompletedEvent>> BuildContext(TransactionCompletedEvent evt)
    {
        var mock = new Mock<ConsumeContext<TransactionCompletedEvent>>();
        mock.Setup(c => c.Message).Returns(evt);
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    // ─── Deposit ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deposit_SendsEmailSmsAndPush_CreatesThreeNotifications()
    {
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), FakeAccountServiceClient.AccountId, null,
            "Deposit", 500m, "USD", "REF001", DateTimeOffset.UtcNow);

        var (consumer, email, sms, push) = BuildConsumer();
        await consumer.Consume(BuildContext(evt).Object);

        email.Sent.Should().HaveCount(1);
        email.Sent[0].To.Should().Be(FakeCustomerServiceClient.Email);
        email.Sent[0].Subject.Should().Contain("Deposit");

        sms.Sent.Should().HaveCount(1);
        sms.Sent[0].To.Should().Be(FakeCustomerServiceClient.Phone);
        sms.Sent[0].Message.Should().Contain("deposited");

        push.Sent.Should().HaveCount(1);
        push.Sent[0].Token.Should().Be(FakeCustomerServiceClient.DeviceToken);

        var notifications = await _db.Notifications.ToListAsync();
        notifications.Should().HaveCount(3);
        notifications.Should().AllSatisfy(n => n.Status.Should().Be(NotificationStatus.Sent));
        notifications.Should().AllSatisfy(n => n.RelatedTransactionId.Should().Be(evt.TransactionId));
    }

    // ─── Withdrawal ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Withdrawal_SendsAllChannels_MessageMentionsWithdrawal()
    {
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), FakeAccountServiceClient.AccountId, null,
            "Withdrawal", 200m, "USD", "REF002", DateTimeOffset.UtcNow);

        var (consumer, email, sms, _) = BuildConsumer();
        await consumer.Consume(BuildContext(evt).Object);

        email.Sent[0].Subject.Should().Contain("Withdrawal");
        sms.Sent[0].Message.Should().Contain("withdrawn");
    }

    // ─── Transfer ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Transfer_SendsAllChannels_MessageMentionsTransfer()
    {
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), FakeAccountServiceClient.AccountId, Guid.NewGuid(),
            "Transfer", 100m, "USD", "REF003", DateTimeOffset.UtcNow);

        var (consumer, email, sms, _) = BuildConsumer();
        await consumer.Consume(BuildContext(evt).Object);

        email.Sent[0].Subject.Should().Contain("Transfer");
        sms.Sent[0].Message.Should().Contain("transferred");
    }

    // ─── No device token ───────────────────────────────────────────────────────

    [Fact]
    public async Task NoDeviceToken_SkipsPush_CreatesOnlyTwoNotifications()
    {
        var noTokenClient = new FakeCustomerServiceClientNoToken();
        var email = new SpyEmailService();
        var sms = new SpySmsService();
        var push = new SpyPushNotificationService();
        var repo = new NotificationRepository(_db);

        var consumer = new TransactionCompletedConsumer(
            new FakeAccountServiceClient(),
            noTokenClient,
            email, sms, push,
            repo,
            NullLogger<TransactionCompletedConsumer>.Instance);

        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), FakeAccountServiceClient.AccountId, null,
            "Deposit", 50m, "USD", "REF004", DateTimeOffset.UtcNow);

        await consumer.Consume(BuildContext(evt).Object);

        push.Sent.Should().BeEmpty();
        email.Sent.Should().HaveCount(1);
        sms.Sent.Should().HaveCount(1);

        var notifications = await _db.Notifications.Where(n => n.RelatedTransactionId == evt.TransactionId).ToListAsync();
        notifications.Should().HaveCount(2);
    }

    // ─── Account not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task AccountNotFound_NoNotificationsSent()
    {
        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Deposit", 100m, "USD", "REF005", DateTimeOffset.UtcNow);

        var (consumer, email, sms, push) = BuildConsumer();
        await consumer.Consume(BuildContext(evt).Object);

        email.Sent.Should().BeEmpty();
        sms.Sent.Should().BeEmpty();
        push.Sent.Should().BeEmpty();
    }

    // ─── Customer not found ────────────────────────────────────────────────────

    [Fact]
    public async Task CustomerNotFound_NoNotificationsSent()
    {
        // Account exists but points to an unknown customer
        var unknownAccountClient = new FakeAccountServiceClientUnknownCustomer();
        var email = new SpyEmailService();
        var sms = new SpySmsService();
        var push = new SpyPushNotificationService();
        var repo = new NotificationRepository(_db);

        var consumer = new TransactionCompletedConsumer(
            unknownAccountClient,
            new FakeCustomerServiceClient(),
            email, sms, push,
            repo,
            NullLogger<TransactionCompletedConsumer>.Instance);

        var evt = new TransactionCompletedEvent(
            Guid.NewGuid(), FakeAccountServiceClientUnknownCustomer.AccountId, null,
            "Deposit", 100m, "USD", "REF006", DateTimeOffset.UtcNow);

        await consumer.Consume(BuildContext(evt).Object);

        email.Sent.Should().BeEmpty();
        sms.Sent.Should().BeEmpty();
        push.Sent.Should().BeEmpty();
    }
}

// ─── Helper fakes scoped to these tests ────────────────────────────────────────

file class FakeCustomerServiceClientNoToken : AtlasBank.NotificationService.Infrastructure.ICustomerServiceClient
{
    public Task<AtlasBank.NotificationService.Infrastructure.CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
        => Task.FromResult<AtlasBank.NotificationService.Infrastructure.CustomerDto?>(
            new AtlasBank.NotificationService.Infrastructure.CustomerDto(
                FakeAccountServiceClient.CustomerId, "Jane", "Doe",
                FakeCustomerServiceClient.Email, FakeCustomerServiceClient.Phone,
                DeviceToken: null));
}

file class FakeAccountServiceClientUnknownCustomer : AtlasBank.NotificationService.Infrastructure.IAccountServiceClient
{
    public static readonly Guid AccountId = Guid.NewGuid();

    public Task<AtlasBank.NotificationService.Infrastructure.AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        if (accountId == AccountId)
            return Task.FromResult<AtlasBank.NotificationService.Infrastructure.AccountDto?>(
                new AtlasBank.NotificationService.Infrastructure.AccountDto(AccountId, Guid.NewGuid(), "ATL9999999", 0, 100m, "USD"));
        return Task.FromResult<AtlasBank.NotificationService.Infrastructure.AccountDto?>(null);
    }
}
