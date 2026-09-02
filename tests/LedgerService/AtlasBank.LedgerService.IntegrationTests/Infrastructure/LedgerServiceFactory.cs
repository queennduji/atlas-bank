using System.Text;
using AtlasBank.LedgerService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace AtlasBank.LedgerService.IntegrationTests.Infrastructure;

public class LedgerServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgresContainer.StartAsync();

    public new async Task DisposeAsync() => await _postgresContainer.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LedgerDbContext>>();
            services.AddDbContext<LedgerDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));

            // Endpoint tests exercise the HTTP surface only – keep MassTransit out of the
            // container so it never tries to reach a real RabbitMQ broker.
            var massTransitHosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true)
                .ToList();
            foreach (var svc in massTransitHosted) services.Remove(svc);
            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton<IPublishEndpoint>(sp => null!);

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
            var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            db.Database.Migrate();
        });
    }
}
