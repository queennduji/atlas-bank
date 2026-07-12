using System.Text;
using AtlasBank.Grpc;
using AtlasBank.StatementService.Data;
using AtlasBank.StatementService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace AtlasBank.StatementService.IntegrationTests.Infrastructure;

public class StatementServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeAccountServiceClient FakeAccountClient { get; } = new();
    public FakeCustomerServiceClient FakeCustomerClient { get; } = new();
    public FakeTransactionServiceClient FakeTransactionClient { get; } = new();

    public async Task InitializeAsync() => await _sqlContainer.StartAsync();

    public new async Task DisposeAsync() => await _sqlContainer.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DB
            services.RemoveAll<DbContextOptions<StatementDbContext>>();
            services.AddDbContext<StatementDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace AccountService gRPC client + interface
            services.RemoveAll<AccountGrpcService.AccountGrpcServiceClient>();
            services.RemoveAll<IAccountServiceClient>();
            services.AddSingleton<IAccountServiceClient>(FakeAccountClient);

            // Replace CustomerService gRPC client + interface
            services.RemoveAll<CustomerGrpcService.CustomerGrpcServiceClient>();
            services.RemoveAll<ICustomerServiceClient>();
            services.AddSingleton<ICustomerServiceClient>(FakeCustomerClient);

            // Replace TransactionService gRPC client + interface
            services.RemoveAll<TransactionGrpcService.TransactionGrpcServiceClient>();
            services.RemoveAll<ITransactionServiceClient>();
            services.AddSingleton<ITransactionServiceClient>(FakeTransactionClient);

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
            var db = scope.ServiceProvider.GetRequiredService<StatementDbContext>();
            db.Database.Migrate();
        });
    }
}
