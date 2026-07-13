using AtlasBank.Grpc;
using AtlasBank.Shared.Middleware;
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<DepositValidator>();

builder.Services.AddGrpcClient<AccountGrpcService.AccountGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["AccountService:GrpcUrl"]!);
});
builder.Services.AddScoped<IAccountServiceClient, AccountServiceClient>();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<TransactionDbContext>(o =>
    {
        o.UseSqlServer();
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
    });

builder.Services.AddAuthorization();
builder.Services.AddGlobalExceptionHandling();
builder.Services.AddGrpc();

var app = builder.Build();

if (app.Environment.IsDevelopment())
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
app.MapGrpcService<TransactionGrpcServer>();

app.Run();

public partial class Program { }




