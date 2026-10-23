using Cine.Application.Interfaces;
using Cine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CineDbContext>(options =>
              options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Mapear la interfaz ICineDbContext hacia el DbContext
        services.AddScoped<ICineDbContext>(provider => provider.GetRequiredService<CineDbContext>());
        return services;
    }
}