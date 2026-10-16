using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Interfaces;

public interface ICineDbContext
{
    DbSet<Vendedor> Vendedors { get; set; }
    DbSet<Pelicula> Peliculas { get; set; }
    DbSet<Funcion> Funcions { get; set; }
    DbSet<Sala> Salas { get; set; }
    DbSet<RedesAutorizada> RedesAutorizadas { get; set; }
    DbSet<EstadoButacaFuncion> EstadoButacaFuncions { get; set; }
    DbSet<Preorden> Preordens { get; set; }
    DbSet<DetallePreordenBoleto> DetallePreordenBoletos { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}