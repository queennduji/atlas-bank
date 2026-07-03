using AtlasBank.NotificationService.Infrastructure;

namespace AtlasBank.NotificationService.IntegrationTests.Infrastructure;

public class FakeCustomerServiceClient : ICustomerServiceClient
{
    public static readonly Guid CustomerId = FakeAccountServiceClient.CustomerId;
    public const string Email = "jane.doe@example.com";
    public const string Phone = "+1234567890";
    public const string DeviceToken = "fake-device-token-abc123";

    public Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        if (customerId == CustomerId)
            return Task.FromResult<CustomerDto?>(new CustomerDto(CustomerId, "Jane", "Doe", Email, Phone, DeviceToken));

        return Task.FromResult<CustomerDto?>(null);
    }
}
