namespace AtlasBank.NotificationService.Infrastructure;

public record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string PhoneNumber, string? DeviceToken = null);

public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default);
}
