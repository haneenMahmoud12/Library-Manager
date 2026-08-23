using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;
using PersonalLibrary.Infrastructure.Identity;
using PersonalLibrary.Infrastructure.Persistence;

namespace PersonalLibrary.Infrastructure.Authentication;

internal sealed class JwtTokenService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options)
    : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<TokenPairResult> IssueAsync(
        AuthenticatedUserResult user,
        CancellationToken cancellationToken = default)
    {
        if (!user.Succeeded)
            throw new ArgumentException("A successfully authenticated user is required.", nameof(user));

        var now = DateTime.UtcNow;
        var refreshToken = CreateRefreshToken(user.UserId!.Value, Guid.NewGuid(), now);
        context.RefreshTokens.Add(refreshToken.StoredToken);
        await context.SaveChangesAsync(cancellationToken);

        return CreateTokenPair(user, refreshToken.RawToken, refreshToken.StoredToken.ExpiresAtUtc, now);
    }

    public async Task<TokenPairResult?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var stored = await context.RefreshTokens.SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);

            if (stored is null)
                return null;

            var now = DateTime.UtcNow;
            if (stored.RevokedAtUtc is not null)
            {
                // Reuse of a rotated token revokes the entire session family.
                if (stored.ReplacedByTokenId is not null)
                {
                    await context.RefreshTokens
                        .Where(token =>
                            token.FamilyId == stored.FamilyId &&
                            token.RevokedAtUtc == null)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(
                                token => token.RevokedAtUtc,
                                now),
                            cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }

                return null;
            }

            if (stored.ExpiresAtUtc <= now)
                return null;

            var identityUser = await userManager.FindByIdAsync(stored.UserId.ToString());
            if (identityUser is null || !identityUser.EmailConfirmed)
                return null;

            var roles = await userManager.GetRolesAsync(identityUser);
            var authenticatedUser = new AuthenticatedUserResult(
                identityUser.Id,
                identityUser.Email,
                identityUser.DisplayName,
                roles.ToArray(),
                []);

            var replacement = CreateRefreshToken(stored.UserId, stored.FamilyId, now);
            stored.RevokedAtUtc = now;
            stored.ReplacedByTokenId = replacement.StoredToken.Id;
            context.RefreshTokens.Add(replacement.StoredToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CreateTokenPair(
                authenticatedUser,
                replacement.RawToken,
                replacement.StoredToken.ExpiresAtUtc,
                now);
        });
    }

    private TokenPairResult CreateTokenPair(
        AuthenticatedUserResult user,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc,
        DateTime now)
    {
        EnsureJwtConfigured();
        var accessTokenExpiresAtUtc = now.AddMinutes(_options.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId!.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(now).ToString(),
                ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.Name!),
            new(ClaimTypes.NameIdentifier, user.UserId.Value.ToString()),
            new(ClaimTypes.Name, user.Name!),
            new(ClaimTypes.Email, user.Email!)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessTokenExpiresAtUtc,
            signingCredentials: credentials);

        return new TokenPairResult(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            accessTokenExpiresAtUtc,
            refreshToken,
            refreshTokenExpiresAtUtc);
    }

    private (string RawToken, RefreshToken StoredToken) CreateRefreshToken(
        Guid userId,
        Guid familyId,
        DateTime now)
    {
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return (
            rawToken,
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = HashToken(rawToken),
                FamilyId = familyId,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays)
            });
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private void EnsureJwtConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Issuer) ||
            string.IsNullOrWhiteSpace(_options.Audience) ||
            Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Issuer, Jwt:Audience, and a signing key of at least 32 bytes must be configured.");
        }

        if (_options.AccessTokenMinutes is < 1 or > 60 ||
            _options.RefreshTokenDays is < 1 or > 90)
        {
            throw new InvalidOperationException("JWT token lifetimes are outside the allowed range.");
        }
    }
}
