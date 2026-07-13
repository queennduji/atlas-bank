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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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




