using AtlasBank.NotificationService.Data.Repositories;
using AtlasBank.NotificationService.Domain.Entities;

namespace AtlasBank.NotificationService.Features.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").RequireAuthorization();

        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/customer/{customerId:guid}", GetByCustomer);
    }

    private static async Task<IResult> GetById(Guid id, INotificationRepository repo, CancellationToken ct)
    {
        var notification = await repo.GetByIdAsync(id, ct);
        if (notification is null) return Results.NotFound();
        return Results.Ok(MapToResponse(notification));
    }

    private static async Task<IResult> GetByCustomer(Guid customerId, INotificationRepository repo, CancellationToken ct)
    {
        var notifications = await repo.GetByCustomerIdAsync(customerId, ct);
        return Results.Ok(notifications.Select(MapToResponse));
    }

    private static NotificationResponse MapToResponse(Notification n) => new(
        n.Id, n.CustomerId, n.Recipient, n.Channel, n.Status,
        n.Subject, n.Body, n.RelatedTransactionId, n.FailureReason,
        n.CreatedAt, n.SentAt);
}
