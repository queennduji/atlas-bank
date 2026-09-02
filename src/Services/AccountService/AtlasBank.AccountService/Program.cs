using AtlasBank.AccountService.Data;
using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Features.Accounts;
using AtlasBank.AccountService.Grpc;
using AtlasBank.AccountService.Infrastructure;
using AtlasBank.Grpc;
using AtlasBank.Shared.Middleware;
using AtlasBank.Shared.Resilience;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAccountValidator>();

// Read-only RPC (GetCustomerByKeycloakId) – safe for the full standard resilience
// pipeline (timeout, retry with backoff, circuit breaker) since retrying a read can't
// double-apply anything.
builder.Services.AddGrpcClient<CustomerGrpcService.CustomerGrpcServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["CustomerService:GrpcUrl"]!)).AddGrpcResilienceHandler();
builder.Services.AddScoped<ICustomerServiceClient, CustomerServiceClient>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        // Lets the OIDC discovery/JWKS fetch happen over a different address than the
        // token issuer (e.g. Keycloak's Docker-network hostname vs. the browser-facing
        // host:port baked into the "iss" claim). Falls back to Authority when unset.
        var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
        if (!string.IsNullOrEmpty(metadataAddress)) options.MetadataAddress = metadataAddress;
    });

builder.Services.AddAuthorization();
builder.Services.AddGrpc();
builder.Services.AddGlobalExceptionHandling();

// Checks real DB connectivity, not just "the process is alive" – a plain liveness
// check would have reported this service as fine even while its database was
// silently never migrated (the bug this replaces the risk of hitting again).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AccountDbContext>();

var app = builder.Build();

{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    db.Database.Migrate();
}

app.UseCorrelationId();
app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<AtlasBank.AccountService.Grpc.AccountGrpcService>();
app.MapAccountEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }



