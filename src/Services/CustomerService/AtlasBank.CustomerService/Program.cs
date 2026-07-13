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

builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    });

builder.Services.AddAuthorization();
builder.Services.AddGrpc();
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    db.Database.Migrate();
}

app.UseGlobalExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();

app.MapCustomerEndpoints();
app.MapGrpcService<CustomerGrpcServer>();

app.Run();

public partial class Program { }
