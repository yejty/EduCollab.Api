using EduCollab.Api.ExceptionHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace EduCollab.Api.Extensions
{
    public static class ApiHostServiceCollectionExtensions
    {
        public static IServiceCollection AddApiHost(this IServiceCollection services)
        {
            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddProblemDetails();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
