using Microsoft.Extensions.DependencyInjection;
using Cine.Application.Interfaces;
using Cine.Application.Services;

namespace Cine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Registro de servicios de aplicación (AuthService, TaquillaService, DashboardFinancieroService, InventarioService, ConfiguracionAdminService)
        services.AddSingleton<IOtpCodeStore>(_ => new InMemoryOtpCodeStore());
        services.AddSingleton<ICineClock, MexicoCityClock>();
        services.AddSingleton<ITematicaEventBroadcaster, TematicaEventBroadcaster>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITaquillaService, TaquillaService>();
        services.AddScoped<IDashboardFinancieroService, DashboardFinancieroService>();
        services.AddScoped<IInventarioService, InventarioService>();
        services.AddScoped<IConfiguracionAdminService, ConfiguracionAdminService>();

        return services;
    }
}
