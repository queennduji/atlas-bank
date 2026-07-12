using AtlasBank.StatementService.Infrastructure;

namespace AtlasBank.StatementService.IntegrationTests.Infrastructure;

public class FakeCustomerServiceClient : ICustomerServiceClient
{
    public static readonly Guid CustomerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    public Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        if (customerId == CustomerId)
            return Task.FromResult<CustomerDto?>(
                new CustomerDto(CustomerId, "Jane", "Doe", "jane@example.com", "+1234567890"));

        return Task.FromResult<CustomerDto?>(null);
    }
}
