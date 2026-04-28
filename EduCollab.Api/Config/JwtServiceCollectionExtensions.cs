using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EduCollab.Api.Config
{
    public static class JwtServiceCollectionExtensions
    {
       public static IServiceCollection AddJwtOptions(this IServiceCollection services, IConfiguration configuration)
        {
              services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer((serviceProvider, options) =>
                    {
                        var jwtOptions = serviceProvider
                            .GetRequiredService<IOptions<JwtOptions>>()
                            .Value;
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
