using AtlasBank.CustomerService.Data;
using AtlasBank.CustomerService.Data.Repositories;
using AtlasBank.CustomerService.Features.Customers;
using AtlasBank.CustomerService.Grpc;
using AtlasBank.CustomerService.Infrastructure;
using AtlasBank.Shared.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCustomerValidator>();

builder.Services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Keycloak:BaseUrl"]!);
});

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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CustomerDbContext>();

var app = builder.Build();

{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    db.Database.Migrate();
}

app.UseCorrelationId();
app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapCustomerEndpoints();
app.MapHealthChecks("/health");
app.MapGrpcService<CustomerGrpcServer>();

app.Run();

public partial class Program { }



