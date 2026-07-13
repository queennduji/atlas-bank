using AtlasBank.AccountService.Data;
using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Features.Accounts;
using AtlasBank.AccountService.Features.Internal;
using AtlasBank.AccountService.Grpc;
using AtlasBank.AccountService.Infrastructure;
using AtlasBank.Grpc;
using AtlasBank.Shared.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAccountValidator>();

builder.Services.AddGrpcClient<CustomerGrpcService.CustomerGrpcServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["CustomerService:GrpcUrl"]!));
builder.Services.AddScoped<ICustomerServiceClient, CustomerServiceClient>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddGrpc();
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    db.Database.Migrate();
}

app.UseGlobalExceptionHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<AtlasBank.AccountService.Grpc.AccountGrpcService>();
app.MapAccountEndpoints();
app.MapInternalAccountEndpoints();

app.Run();

public partial class Program { }


