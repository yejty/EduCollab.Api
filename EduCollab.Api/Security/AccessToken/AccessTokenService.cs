using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduCollab.Api.Config;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduCollab.Api.Security.AccessToken
{
    public sealed class AccessTokenService : IAccessTokenService
    {
        private readonly JwtOptions _options;

        public AccessTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string Create(int userId, string email)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
            };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
