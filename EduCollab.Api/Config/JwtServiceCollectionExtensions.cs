using EduCollab.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                        options.TokenValidationParameters = JwtTokenValidationParametersFactory.Create(jwtOptions);
                    });
                services.AddAuthorization();
                return services;

        }
    }
}
