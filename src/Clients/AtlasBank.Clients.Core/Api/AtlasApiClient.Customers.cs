using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Clients.Core.Api;

public sealed partial class AtlasApiClient
{
    // public route, no token needed yet — this is how the customer gets created in the first place
    public Task<Customer> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken ct = default) =>
        PostAsync<Customer>("/api/customers/register", request, ct);

    public Task<Customer> GetMyProfileAsync(CancellationToken ct = default) =>
        GetAsync<Customer>("/api/customers/me", ct);

    public Task<Customer> UpdateMyProfileAsync(UpdateCustomerRequest request, CancellationToken ct = default) =>
        PutAsync<Customer>("/api/customers/me", request, ct);
}
