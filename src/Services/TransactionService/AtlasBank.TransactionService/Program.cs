using AtlasBank.Grpc;
using AtlasBank.Shared.Middleware;
using AtlasBank.Shared.Resilience;
using AtlasBank.TransactionService.Data;
using AtlasBank.TransactionService.Data.Repositories;
using AtlasBank.TransactionService.Features.Transactions;
using AtlasBank.TransactionService.Grpc;
using AtlasBank.TransactionService.Infrastructure;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<DepositValidator>();

// This client carries both a read (GetAccount) and the non-idempotent Credit/Debit
// writes, so it gets timeout + circuit breaker but NOT automatic retry: if a Credit
// call's response is lost after the write already landed, blindly retrying it would
// apply the credit twice. (The other services' AccountGrpcServiceClient registrations
// only ever call GetAccount and get the full pipeline including retry — see their
// Program.cs for why that's safe there.)
builder.Services.AddGrpcClient<AccountGrpcService.AccountGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["AccountService:GrpcUrl"]!);
}).AddGrpcResilienceHandler(allowRetry: false);
builder.Services.AddScoped<IAccountServiceClient, AccountServiceClient>();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<TransactionDbContext>(o =>
    {
        o.UsePostgres();
        o.QueryDelay = TimeSpan.FromSeconds(10);
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.UseMessageRetry(r => r.Intervals(500, 1000, 2000));
        cfg.ConfigureEndpoints(ctx);
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
    .AddDbContextCheck<TransactionDbContext>();
builder.Services.AddGrpc();

var app = builder.Build();

{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    db.Database.Migrate();
}

app.UseCorrelationId();
app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapTransactionEndpoints();
app.MapHealthChecks("/health");
app.MapGrpcService<TransactionGrpcServer>();

app.Run();

public partial class Program { }




