using AtlasBank.Grpc;

namespace AtlasBank.AccountService.Infrastructure;

public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default);
}

public record CustomerDto(Guid Id, string FirstName, string LastName, string Email);

public class CustomerServiceClient(CustomerGrpcService.CustomerGrpcServiceClient grpcClient) : ICustomerServiceClient
{
    public async Task<CustomerDto?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default)
    {
        var reply = await grpcClient.GetCustomerByKeycloakIdAsync(
            new GetCustomerByKeycloakIdRequest { KeycloakUserId = keycloakUserId }, cancellationToken: ct);
        if (!reply.Found) return null;
        return new CustomerDto(Guid.Parse(reply.Id), reply.FirstName, reply.LastName, reply.Email);
    }
}
