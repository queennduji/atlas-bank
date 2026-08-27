namespace AtlasBank.Clients.Core.Models;

public sealed record Address
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public required string Country { get; init; }
}

public sealed record RegisterCustomerRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string PhoneNumber { get; init; }

    /// <summary>yyyy-MM-dd, matching the frontend's plain date-input string.</summary>
    public required string DateOfBirth { get; init; }

    public required Address Address { get; init; }
}

public sealed record UpdateCustomerRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string PhoneNumber { get; init; }
    public required Address Address { get; init; }
}

public sealed record Customer
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required string DateOfBirth { get; init; }
    public required Address Address { get; init; }
    public required CustomerStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public string FullName => $"{FirstName} {LastName}";
}
