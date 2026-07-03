using AtlasBank.Grpc;
using AtlasBank.TransactionService.Data;
using AtlasBank.TransactionService.Data.Repositories;
using AtlasBank.TransactionService.Features.Transactions;
using AtlasBank.TransactionService.Infrastructure;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapTransactionEndpoints();

app.Run();

public partial class Program { }
