using System.Text;
using AtlasBank.Grpc;
using AtlasBank.TransactionService.Data;
using AtlasBank.TransactionService.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace AtlasBank.TransactionService.IntegrationTests.Infrastructure;

public class TransactionServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeAccountServiceClient FakeAccountClient { get; } = new();

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
            // Replace real DB
            services.RemoveAll<DbContextOptions<TransactionDbContext>>();
            services.AddDbContext<TransactionDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace gRPC client registration with fake HTTP client
            services.RemoveAll<AccountGrpcService.AccountGrpcServiceClient>();
            services.RemoveAll<IAccountServiceClient>();
            services.AddSingleton<IAccountServiceClient>(FakeAccountClient);

            // Prevent MassTransit from starting (no RabbitMQ in tests)
            // Remove the hosted service that connects to RabbitMQ, then override IPublishEndpoint
            var massTransitHosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            (d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true ||
                             d.ImplementationFactory != null))
                .ToList();
            foreach (var svc in massTransitHosted) services.Remove(svc);

            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton<IPublishEndpoint, NoOpPublishEndpoint>();

            // Replace JWT Bearer with test key
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
            var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            db.Database.Migrate();
        });
    }
}
