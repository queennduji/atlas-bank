using AtlasBank.LedgerService.Data;
using AtlasBank.LedgerService.Data.Repositories;
using AtlasBank.LedgerService.Features.Ledger;
using AtlasBank.LedgerService.Messaging.Consumers;
using AtlasBank.Shared.Middleware;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddDbContext<LedgerDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TransactionCompletedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
        // Explicit, service-scoped endpoint name – see NotificationService's Program.cs
        // for why this can't be left to MassTransit's default per-consumer-type naming.
        cfg.ReceiveEndpoint("ledger-service-transaction-completed", e =>
        {
            e.ConfigureConsumer<TransactionCompletedConsumer>(ctx);
        });
    });
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
builder.Services.AddGlobalExceptionHandling();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LedgerDbContext>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    db.Database.Migrate();
}

app.UseCorrelationId();
app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapLedgerEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
