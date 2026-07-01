using System.Net;
using System.Net.Http.Json;
using AtlasBank.CustomerService.Features.Customers;
using AtlasBank.CustomerService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AtlasBank.CustomerService.IntegrationTests;

public class CustomerRegistrationTests(CustomerServiceFactory factory)
    : IClassFixture<CustomerServiceFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static RegisterCustomerRequest ValidRequest(string email = "john.doe@example.com") => new(
        FirstName: "John",
        LastName: "Doe",
        Email: email,
        Password: "SecurePass1!",
        PhoneNumber: "+12345678901",
        DateOfBirth: new DateOnly(1990, 1, 15),
        Address: new AddressDto("123 Main St", "New York", "NY", "10001", "US")
    );

    [Fact]
    public async Task Register_WithValidRequest_Returns201WithCustomerProfile()
    {
        var response = await _client.PostAsJsonAsync("/api/customers/register", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.FirstName.Should().Be("John");
        customer.LastName.Should().Be("Doe");
        customer.Email.Should().Be("john.doe@example.com");
        customer.Status.Should().Be(Domain.Enums.CustomerStatus.Active);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var request = ValidRequest("duplicate@example.com");
        await _client.PostAsJsonAsync("/api/customers/register", request);

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400WithValidationError()
    {
        var request = ValidRequest() with { Email = "not-an-email" };

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("valid email");
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400WithValidationError()
    {
        var request = ValidRequest() with { Password = "weak" };

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("8 characters");
    }

    [Fact]
    public async Task Register_UnderAge_Returns400WithValidationError()
    {
        var request = ValidRequest() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)) };

        var response = await _client.PostAsJsonAsync("/api/customers/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("18 years");
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCustomerProfile()
    {
        const string email = "getme@example.com";

        await _client.PostAsJsonAsync("/api/customers/register", ValidRequest(email));

        // FakeKeycloakAdminClient uses email as the Keycloak user ID
        var token = TestJwtTokenGenerator.GenerateToken(email, email);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/customers/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer!.Email.Should().Be(email);
        customer.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/customers/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
