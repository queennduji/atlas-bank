using AtlasBank.CustomerService.Domain.Enums;
using AtlasBank.CustomerService.Domain.ValueObjects;

namespace AtlasBank.CustomerService.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string KeycloakUserId { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public DateOnly DateOfBirth { get; private set; }
    public Address Address { get; private set; } = default!;
    public CustomerStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Customer() { }

    public static Customer Create(
        string keycloakUserId,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        DateOnly dateOfBirth,
        Address address)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth,
            Address = address,
            Status = CustomerStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string phoneNumber,
        Address address)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Address = address;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        if (Status != CustomerStatus.Active)
            throw new InvalidOperationException("Only active customers can be suspended.");
        Status = CustomerStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
