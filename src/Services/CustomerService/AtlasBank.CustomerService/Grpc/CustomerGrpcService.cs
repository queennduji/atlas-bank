using AtlasBank.CustomerService.Data.Repositories;
using AtlasBank.Grpc;
using Grpc.Core;

namespace AtlasBank.CustomerService.Grpc;

public class CustomerGrpcServer(ICustomerRepository repo) : AtlasBank.Grpc.CustomerGrpcService.CustomerGrpcServiceBase
{
    public override async Task<CustomerReply> GetCustomer(GetCustomerRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CustomerId, out var id))
            return new CustomerReply { Found = false };

        var customer = await repo.GetByIdAsync(id, context.CancellationToken);
        if (customer is null) return new CustomerReply { Found = false };

        return new CustomerReply
        {
            Found = true,
            Id = customer.Id.ToString(),
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber
        };
    }

    public override async Task<CustomerReply> GetCustomerByKeycloakId(GetCustomerByKeycloakIdRequest request, ServerCallContext context)
    {
        var customer = await repo.GetByKeycloakUserIdAsync(request.KeycloakUserId, context.CancellationToken);
        if (customer is null) return new CustomerReply { Found = false };

        return new CustomerReply
        {
            Found = true,
            Id = customer.Id.ToString(),
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber
        };
    }
}
