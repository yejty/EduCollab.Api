using System.Text;
using Drive.Api.Config;
using Microsoft.IdentityModel.Tokens;

namespace Drive.Api.Security
{
    internal static class JwtTokenValidationParametersFactory
    {
        public static TokenValidationParameters Create(JwtOptions jwtOptions) =>
            new()
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
    }
}
