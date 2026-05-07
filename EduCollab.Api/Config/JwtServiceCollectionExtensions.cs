using EduCollab.Api.Security.AccessToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EduCollab.Api.Config
{
    public static class JwtServiceCollectionExtensions
    {
       public static IServiceCollection AddJwtOptions(this IServiceCollection services, IConfiguration configuration)
        {
              services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
              services.AddSingleton<IAccessTokenService, AccessTokenService>();
              services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                            ?? throw new InvalidOperationException("JWT options are not configured.");
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwtOptions.Issuer,
                            ValidateAudience = true,
                            ValidAudience = jwtOptions.Audience,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero
                        };
                    });
                services.AddAuthorization();
                return services;

        }
    }
}
