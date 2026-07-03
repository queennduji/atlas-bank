using AtlasBank.CustomerService.Data.Repositories;
using AtlasBank.CustomerService.Domain.Entities;
using AtlasBank.CustomerService.Domain.ValueObjects;
using AtlasBank.CustomerService.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AtlasBank.CustomerService.Features.Customers;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        // Registration is public — user has no token yet
        app.MapPost("/api/customers/register", Register);

        var group = app.MapGroup("/api/customers").RequireAuthorization();
        group.MapGet("/me", GetMe);
        group.MapPut("/me", UpdateMe);

        // Internal endpoints for service-to-service calls — not exposed via API Gateway
        app.MapGet("/internal/customers/{id:guid}", GetById);
        app.MapGet("/internal/customers/by-keycloak-id/{keycloakUserId}", GetByKeycloakId);
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterCustomerRequest request,
        ICustomerRepository repo,
        IKeycloakAdminClient keycloak,
        IValidator<RegisterCustomerRequest> validator,
        CancellationToken ct)
    {
        var validationError = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationError is not null) return validationError;

        if (await repo.ExistsByEmailAsync(request.Email, ct))
            return Results.Conflict("A customer with this email already exists.");

        string keycloakUserId;
        try
        {
            keycloakUserId = await keycloak.CreateUserAsync(
                request.FirstName, request.LastName, request.Email, request.Password, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }

        var customer = Customer.Create(
            keycloakUserId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            new Address(request.Address.Street, request.Address.City,
                        request.Address.State, request.Address.ZipCode,
                        request.Address.Country)
        );

        await repo.AddAsync(customer, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Created($"/api/customers/{customer.Id}", ToResponse(customer));
    }

    private static async Task<IResult> GetMe(
        ICustomerRepository repo,
        HttpContext http,
        CancellationToken ct)
    {
        var keycloakUserId = http.User.FindFirst("sub")?.Value;
        if (keycloakUserId is null) return Results.Unauthorized();

        var customer = await repo.GetByKeycloakUserIdAsync(keycloakUserId, ct);
        if (customer is null) return Results.NotFound();

        return Results.Ok(ToResponse(customer));
    }

    private static async Task<IResult> UpdateMe(
        [FromBody] UpdateCustomerRequest request,
        ICustomerRepository repo,
        IValidator<UpdateCustomerRequest> validator,
        HttpContext http,
        CancellationToken ct)
    {
        var validationError = await ValidationHelper.ValidateAsync(validator, request, ct);
        if (validationError is not null) return validationError;

        var keycloakUserId = http.User.FindFirst("sub")?.Value;
        if (keycloakUserId is null) return Results.Unauthorized();

        var customer = await repo.GetByKeycloakUserIdAsync(keycloakUserId, ct);
        if (customer is null) return Results.NotFound();

        customer.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            new Address(request.Address.Street, request.Address.City,
                        request.Address.State, request.Address.ZipCode,
                        request.Address.Country)
        );

        await repo.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(customer));
    }

    private static async Task<IResult> GetById(
        Guid id,
        ICustomerRepository repo,
        CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(id, ct);
        if (customer is null) return Results.NotFound();
        return Results.Ok(ToResponse(customer));
    }

    private static async Task<IResult> GetByKeycloakId(
        string keycloakUserId,
        ICustomerRepository repo,
        CancellationToken ct)
    {
        var customer = await repo.GetByKeycloakUserIdAsync(keycloakUserId, ct);
        if (customer is null) return Results.NotFound();
        return Results.Ok(ToResponse(customer));
    }

    private static CustomerResponse ToResponse(Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.DateOfBirth,
            new AddressDto(c.Address.Street, c.Address.City, c.Address.State,
                           c.Address.ZipCode, c.Address.Country),
            c.Status, c.CreatedAt);
}
