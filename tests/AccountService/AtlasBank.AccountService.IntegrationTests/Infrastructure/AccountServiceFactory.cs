using AtlasBank.AccountService.Data;
using AtlasBank.AccountService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Testcontainers.MsSql;

namespace AtlasBank.AccountService.IntegrationTests.Infrastructure;

public class AccountServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Guid DefaultCustomerId { get; } = Guid.NewGuid();
    public string DefaultKeycloakUserId { get; } = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AccountDbContext>>();
            services.AddDbContext<AccountDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace CustomerServiceClient with a fake that returns test customer
            services.RemoveAll<ICustomerServiceClient>();
            services.AddSingleton<ICustomerServiceClient>(
                new FakeCustomerServiceClient(DefaultKeycloakUserId, DefaultCustomerId));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.Audience = TestJwtTokenGenerator.TestAudience;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestJwtTokenGenerator.TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestJwtTokenGenerator.TestAudience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(TestJwtTokenGenerator.TestSigningKey)),
                    NameClaimType = "sub"
                };
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
            db.Database.Migrate();
        });
    }
}

public class FakeCustomerServiceClient(string keycloakUserId, Guid customerId) : ICustomerServiceClient
{
    public Task<CustomerDto?> GetByKeycloakUserIdAsync(string id, CancellationToken ct = default)
    {
        if (id == keycloakUserId)
            return Task.FromResult<CustomerDto?>(new CustomerDto(customerId, "John", "Doe", "john@example.com"));
        return Task.FromResult<CustomerDto?>(null);
    }
}
