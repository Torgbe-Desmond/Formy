using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Formify.Api.Services;

/// <summary>Mirrors Node's services/auth.service.js (JWT + bcrypt).</summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _dbContext;
    public AuthService(IConfiguration config, AppDbContext dbContext)
    {
        _config = config;
        _dbContext = dbContext;
    }

    public string HashPassword(string plaintext) => BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);

    public bool VerifyPassword(string plaintext, string hash) => BCrypt.Net.BCrypt.Verify(plaintext, hash);

    public string GenerateToken(User user)
    {
        var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "Formify";
        var audience = _config["Jwt:Audience"] ?? "Formify";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponse> RegisterUser(RegisterRequest user, CancellationToken ct = default)
    {
        var newUser = new User
        {
            Name = user.Name.Trim(),
            Email = user.Email.Trim().ToLowerInvariant(),
            PasswordHash = this.HashPassword(user.Password),
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync(ct);

        var token = this.GenerateToken(newUser);

        return new AuthResponse(token, new UserDto(newUser.Id, newUser.Name, newUser.Email));
    }

    public async Task<User?> FindByEmail(string email, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        return user;
    }

}
