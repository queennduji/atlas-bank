using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AtlasBank.NotificationService.Data;
using AtlasBank.NotificationService.Domain.Entities;
using AtlasBank.NotificationService.Features.Notifications;
using AtlasBank.NotificationService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AtlasBank.NotificationService.IntegrationTests;

public class NotificationEndpointTests : IClassFixture<NotificationServiceFactory>
{
    private readonly HttpClient _client;
    private readonly NotificationServiceFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public NotificationEndpointTests(NotificationServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        var token = TestJwtTokenGenerator.GenerateToken(Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<Notification> SeedNotificationAsync(Guid? customerId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = Notification.CreateEmail(
            customerId ?? FakeCustomerServiceClient.CustomerId,
            "test@example.com",
            "Test Subject",
            "Test Body",
            Guid.NewGuid());
        notification.MarkSent();

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification;
    }

    // ─── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingNotification_Returns200()
    {
        var seeded = await SeedNotificationAsync();

        var response = await _client.GetAsync($"/api/notifications/{seeded.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<NotificationResponse>(JsonOptions);
        result!.Id.Should().Be(seeded.Id);
        result.Subject.Should().Be("Test Subject");
        result.Status.Should().Be(AtlasBank.NotificationService.Domain.Enums.NotificationStatus.Sent);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/notifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NoToken_Returns401()
    {
        var seeded = await SeedNotificationAsync();
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/notifications/{seeded.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── GetByCustomer ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCustomer_ReturnsNotificationsForCustomer()
    {
        var customerId = Guid.NewGuid();
        await SeedNotificationAsync(customerId);
        await SeedNotificationAsync(customerId);

        var response = await _client.GetAsync($"/api/notifications/customer/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(JsonOptions);
        results.Should().NotBeEmpty();
        results!.Should().AllSatisfy(n => n.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public async Task GetByCustomer_UnknownCustomer_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/notifications/customer/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(JsonOptions);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCustomer_NoToken_Returns401()
    {
        var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/notifications/customer/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
