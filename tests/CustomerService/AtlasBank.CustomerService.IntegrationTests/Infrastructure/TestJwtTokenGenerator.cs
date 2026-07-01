using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AtlasBank.CustomerService.IntegrationTests.Infrastructure;

public static class TestJwtTokenGenerator
{
    public const string TestIssuer = "test-issuer";
    public const string TestAudience = "atlas-bank-app";
    public const string TestSigningKey = "test-signing-key-that-is-long-enough-32chars";

    public static string GenerateToken(string userId, string email = "test@example.com", string role = "user")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim("email", email),
            new Claim("realm_access", $"{{\"roles\":[\"{role}\"]}}"),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
