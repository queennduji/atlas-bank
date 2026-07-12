using System.Text.Json.Serialization;
using AtlasBank.CardService.Data;
using AtlasBank.CardService.Data.Repositories;
using AtlasBank.CardService.Features.Cards;
using AtlasBank.CardService.Infrastructure;
using AtlasBank.Grpc;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICardRepository, CardRepository>();

builder.Services.AddGrpcClient<AccountGrpcService.AccountGrpcServiceClient>(options =>
    options.Address = new Uri(builder.Configuration["AccountService:GrpcUrl"]!));
builder.Services.AddScoped<IAccountServiceClient, AccountServiceClient>();

builder.Services.AddGrpcClient<CustomerGrpcService.CustomerGrpcServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["CustomerService:GrpcUrl"]!));
builder.Services.AddScoped<ICustomerServiceClient, CustomerServiceClient>();

builder.Services.AddValidatorsFromAssemblyContaining<IssueCardValidator>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], builder.Configuration["RabbitMQ:VirtualHost"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
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
    });

builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CardDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCardEndpoints();

app.Run();

public partial class Program { }
