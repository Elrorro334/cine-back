using Microsoft.Extensions.DependencyInjection;
using Cine.Application.Interfaces;
using Cine.Application.Services;

namespace Cine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Registro de servicios de aplicación (AuthService, TaquillaService, DashboardFinancieroService)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITaquillaService, TaquillaService>();
        services.AddScoped<IDashboardFinancieroService, DashboardFinancieroService>();

        return services;
    }
}
