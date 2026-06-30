using AtlasBank.CustomerService.Domain.Enums;

namespace AtlasBank.CustomerService.Features.Customers;

public record RegisterCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber,
    DateOnly DateOfBirth,
    AddressDto Address
);

public record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    AddressDto Address
);

public record AddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);

public record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly DateOfBirth,
    AddressDto Address,
    CustomerStatus Status,
    DateTimeOffset CreatedAt
);
