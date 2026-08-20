using System.Text;
using AtlasBank.Grpc;
using AtlasBank.NotificationService.Data;
using AtlasBank.NotificationService.Infrastructure;
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

namespace AtlasBank.NotificationService.IntegrationTests.Infrastructure;

public class NotificationServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public FakeAccountServiceClient FakeAccountClient { get; } = new();
    public FakeCustomerServiceClient FakeCustomerClient { get; } = new();
    public SpyEmailService SpyEmail { get; } = new();
    public SpySmsService SpySms { get; } = new();
    public SpyPushNotificationService SpyPush { get; } = new();

    public async Task InitializeAsync() => await _postgresContainer.StartAsync();

    public new async Task DisposeAsync() => await _postgresContainer.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DB
            services.RemoveAll<DbContextOptions<NotificationDbContext>>();
            services.AddDbContext<NotificationDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));

            // Replace gRPC client + account service
            services.RemoveAll<AccountGrpcService.AccountGrpcServiceClient>();
            services.RemoveAll<IAccountServiceClient>();
            services.AddSingleton<IAccountServiceClient>(FakeAccountClient);

            // Replace customer service (gRPC client + interface)
            services.RemoveAll<CustomerGrpcService.CustomerGrpcServiceClient>();
            services.RemoveAll<ICustomerServiceClient>();
            services.AddSingleton<ICustomerServiceClient>(FakeCustomerClient);

            // Replace notification senders with spies
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(SpyEmail);
            services.RemoveAll<ISmsService>();
            services.AddSingleton<ISmsService>(SpySms);
            services.RemoveAll<IPushNotificationService>();
            services.AddSingleton<IPushNotificationService>(SpyPush);

            // Prevent MassTransit from connecting to RabbitMQ
            var massTransitHosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true)
                .ToList();
            foreach (var svc in massTransitHosted) services.Remove(svc);
            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton<IPublishEndpoint>(sp => null!);

            // Replace JWT with test key
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
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            db.Database.Migrate();
        });
    }
}
