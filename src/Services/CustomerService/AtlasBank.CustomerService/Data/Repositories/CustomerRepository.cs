using AtlasBank.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.CustomerService.Data.Repositories;

public class CustomerRepository(CustomerDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.KeycloakUserId == keycloakUserId, ct);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.Customers.AnyAsync(c => c.Email == email, ct);

    public Task<bool> ExistsByKeycloakUserIdAsync(string keycloakUserId, CancellationToken ct = default) =>
        db.Customers.AnyAsync(c => c.KeycloakUserId == keycloakUserId, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default) =>
        await db.Customers.AddAsync(customer, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
