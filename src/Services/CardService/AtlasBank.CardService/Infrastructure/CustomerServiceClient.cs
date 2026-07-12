using AtlasBank.Grpc;

namespace AtlasBank.CardService.Infrastructure;

public class CustomerServiceClient(CustomerGrpcService.CustomerGrpcServiceClient grpcClient) : ICustomerServiceClient
{
    public async Task<CustomerDto?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var reply = await grpcClient.GetCustomerAsync(
            new GetCustomerRequest { CustomerId = customerId.ToString() }, cancellationToken: ct);

        if (!reply.Found) return null;

        return new CustomerDto(
            Guid.Parse(reply.Id),
            reply.FirstName,
            reply.LastName,
            reply.Email,
            reply.PhoneNumber);
    }
}
