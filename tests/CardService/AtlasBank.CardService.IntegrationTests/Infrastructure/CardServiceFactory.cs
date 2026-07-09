using System.Text;
using AtlasBank.CardService.Data;
using AtlasBank.CardService.Infrastructure;
using AtlasBank.Grpc;
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

namespace AtlasBank.CardService.IntegrationTests.Infrastructure;

public class CardServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeAccountServiceClient FakeAccountClient { get; } = new();
    public FakeCustomerServiceClient FakeCustomerClient { get; } = new();

    public async Task InitializeAsync() => await _sqlContainer.StartAsync();

    public new async Task DisposeAsync() => await _sqlContainer.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DB
            services.RemoveAll<DbContextOptions<CardDbContext>>();
            services.AddDbContext<CardDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace gRPC client + account service client
            services.RemoveAll<AccountGrpcService.AccountGrpcServiceClient>();
            services.RemoveAll<IAccountServiceClient>();
            services.AddSingleton<IAccountServiceClient>(FakeAccountClient);

            // Replace customer service client
            services.RemoveAll<ICustomerServiceClient>();
            services.AddSingleton<ICustomerServiceClient>(FakeCustomerClient);

            // Prevent MassTransit from connecting to RabbitMQ; replace publish endpoint with no-op
            var massTransitHosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true)
                .ToList();
            foreach (var svc in massTransitHosted) services.Remove(svc);
            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton<IPublishEndpoint, NoOpPublishEndpoint>();

            // Replace JWT with symmetric test key
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
            var db = scope.ServiceProvider.GetRequiredService<CardDbContext>();
            db.Database.Migrate();
        });
    }
}
