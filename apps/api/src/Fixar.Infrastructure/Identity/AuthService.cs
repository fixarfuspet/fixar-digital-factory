using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Fixar.Application.Common.Interfaces;
using Fixar.Application.Common.Models;
using Fixar.Application.Features.Auth;
using Fixar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fixar.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IDateTimeService dateTimeService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _context = context;
        _dateTimeService = dateTimeService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return AuthResult.Fail("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = _dateTimeService.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Fail(createResult.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Guest);

        return await IssueTokensAsync(user, ipAddress: null, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return AuthResult.Fail("Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Fail("This account is locked. Try again later.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return AuthResult.Fail("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var existingToken = await _context.RefreshTokens
            .SingleOrDefaultAsync(t => t.Token == tokenHash || t.Token == refreshToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            return AuthResult.Fail("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return AuthResult.Fail("Invalid or expired refresh token.");
        }

        existingToken.RevokedAtUtc = _dateTimeService.UtcNow;
        existingToken.RevokedByIp = ipAddress;

        var result = await IssueTokensAsync(user, ipAddress, cancellationToken);

        existingToken.ReplacedByToken = HashRefreshToken(result.RefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var existingToken = await _context.RefreshTokens
            .SingleOrDefaultAsync(t => t.Token == tokenHash || t.Token == refreshToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            return;
        }

        existingToken.RevokedAtUtc = _dateTimeService.UtcNow;
        existingToken.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var now = _dateTimeService.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var accessToken = GenerateAccessToken(user, roles, accessTokenExpiresAt);
        var refreshToken = GenerateRefreshTokenValue();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = HashRefreshToken(refreshToken),
            ExpiresAtUtc = now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAtUtc = now,
            CreatedByIp = ipAddress
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList(),
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAt,
            RefreshToken = refreshToken
        };
    }

    private string GenerateAccessToken(ApplicationUser user, IList<string> roles, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshTokenValue()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashRefreshToken(string token)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return $"sha256:{Convert.ToHexString(hash)}";
    }
}
