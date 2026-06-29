using AtlasBank.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.CustomerService.Data;

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.KeycloakUserId).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.KeycloakUserId).IsUnique();
            e.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();
            e.Property(c => c.Status).HasConversion<string>();

            e.OwnsOne(c => c.Address, a =>
            {
                a.Property(x => x.Street).HasMaxLength(200).IsRequired();
                a.Property(x => x.City).HasMaxLength(100).IsRequired();
                a.Property(x => x.State).HasMaxLength(100).IsRequired();
                a.Property(x => x.ZipCode).HasMaxLength(20).IsRequired();
                a.Property(x => x.Country).HasMaxLength(100).IsRequired();
            });
        });
    }
}
