namespace AtlasBank.StatementService.Infrastructure;

public record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string PhoneNumber);

public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default);
}
