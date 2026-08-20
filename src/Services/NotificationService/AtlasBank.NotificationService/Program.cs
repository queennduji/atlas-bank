using AtlasBank.Shared.Middleware;
using System.Text.Json.Serialization;
using AtlasBank.Grpc;
using AtlasBank.NotificationService.Data;
using AtlasBank.NotificationService.Data.Repositories;
using AtlasBank.NotificationService.Features.Notifications;
using AtlasBank.NotificationService.Infrastructure;
using AtlasBank.NotificationService.Messaging.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Account service via gRPC
builder.Services.AddGrpcClient<AccountGrpcService.AccountGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["AccountService:GrpcUrl"]!);
});
builder.Services.AddScoped<IAccountServiceClient, AccountServiceClient>();

// Customer service via gRPC
builder.Services.AddGrpcClient<CustomerGrpcService.CustomerGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["CustomerService:GrpcUrl"]!);
});
builder.Services.AddScoped<ICustomerServiceClient, CustomerServiceClient>();

builder.Services.AddSingleton<IEmailService, ConsoleEmailService>();
builder.Services.AddSingleton<ISmsService, ConsoleSmsService>();
builder.Services.AddSingleton<IPushNotificationService, ConsolePushNotificationService>();

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
        // Explicit, service-scoped endpoint name — MassTransit's default naming
        // convention derives the queue name from the consumer type's short name, and
        // another service's consumer for the same event happening to share that name
        // (e.g. every service calling its consumer "TransactionCompletedConsumer") would
        // otherwise bind both services to the same queue, splitting deliveries between
        // them instead of each service getting its own copy of every event.
        cfg.ReceiveEndpoint("notification-service-transaction-completed", e =>
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

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

app.UseCorrelationId();
app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapNotificationEndpoints();

app.Run();

public partial class Program { }




