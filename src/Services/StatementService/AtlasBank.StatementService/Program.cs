using AtlasBank.Grpc;
using AtlasBank.Shared.Middleware;
using AtlasBank.StatementService.Data;
using AtlasBank.StatementService.Data.Repositories;
using AtlasBank.StatementService.Features.Statements;
using AtlasBank.StatementService.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StatementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IStatementRepository, StatementRepository>();

var accountGrpcUrl = builder.Configuration["AccountService:GrpcUrl"]!;
builder.Services.AddGrpcClient<AccountGrpcService.AccountGrpcServiceClient>(o =>
    o.Address = new Uri(accountGrpcUrl));
builder.Services.AddScoped<IAccountServiceClient, AccountServiceClient>();

var customerGrpcUrl = builder.Configuration["CustomerService:GrpcUrl"]!;
builder.Services.AddGrpcClient<CustomerGrpcService.CustomerGrpcServiceClient>(o =>
    o.Address = new Uri(customerGrpcUrl));
builder.Services.AddScoped<ICustomerServiceClient, CustomerServiceClient>();

var transactionGrpcUrl = builder.Configuration["TransactionService:GrpcUrl"]!;
builder.Services.AddGrpcClient<TransactionGrpcService.TransactionGrpcServiceClient>(o =>
    o.Address = new Uri(transactionGrpcUrl));
builder.Services.AddScoped<ITransactionServiceClient, TransactionServiceClient>();

builder.Services.AddValidatorsFromAssemblyContaining<GenerateStatementRequestValidator>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StatementDbContext>();
    db.Database.Migrate();
}

app.UseGlobalExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();

app.MapStatementEndpoints();

app.Run();

public partial class Program { }
