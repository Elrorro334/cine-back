using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Interfaces;

public interface ICineDbContext
{
    DbSet<Vendedor> Vendedors { get; set; }
    DbSet<Pelicula> Peliculas { get; set; }
    DbSet<RedesAutorizada> RedesAutorizadas { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}