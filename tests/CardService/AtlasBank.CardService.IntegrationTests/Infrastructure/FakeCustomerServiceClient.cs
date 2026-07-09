using AtlasBank.CardService.Infrastructure;

namespace AtlasBank.CardService.IntegrationTests.Infrastructure;

public class FakeCustomerServiceClient : ICustomerServiceClient
{
    public static readonly Guid CustomerId = FakeAccountServiceClient.CustomerId;

    public Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        if (customerId == CustomerId)
            return Task.FromResult<CustomerDto?>(new CustomerDto(CustomerId, "Jane", "Doe", "jane@example.com", "+1234567890"));

        return Task.FromResult<CustomerDto?>(null);
    }
}
