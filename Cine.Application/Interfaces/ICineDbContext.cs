using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Interfaces;

public interface ICineDbContext
{
    DbSet<Vendedor> Vendedors { get; set; }
    DbSet<Pelicula> Peliculas { get; set; }
    DbSet<Funcion> Funcions { get; set; }
    DbSet<Sala> Salas { get; set; }
    DbSet<Butaca> Butacas { get; set; }
    DbSet<RedesAutorizada> RedesAutorizadas { get; set; }
    DbSet<EstadoButacaFuncion> EstadoButacaFuncions { get; set; }
    DbSet<Preorden> Preordens { get; set; }
    DbSet<DetallePreordenBoleto> DetallePreordenBoletos { get; set; }
    DbSet<Ventum> Venta { get; set; }
    DbSet<DetalleVentum> DetalleVenta { get; set; }
    DbSet<Producto> Productos { get; set; }
    DbSet<Categorium> Categoria { get; set; }
    DbSet<DetallePreordenDulcerium> DetallePreordenDulceria { get; set; }
    DbSet<InventarioStockFraccionado> InventarioStockFraccionados { get; set; }
    DbSet<Rol> Rols { get; set; }
    DbSet<Cliente> Clientes { get; set; }
    DbSet<EntradaMercancium> EntradaMercancia { get; set; }
    DbSet<Proveedor> Proveedores { get; set; }
    DbSet<FacturaProveedor> FacturasProveedor { get; set; }
    DbSet<CanjePreorden> CanjePreordens { get; set; }
    DbSet<PagoVenta> PagoVentas { get; set; }
    DbSet<Sucursal> Sucursales { get; set; }
    DbSet<ConfiguracionSeguridad> ConfiguracionesSeguridad { get; set; }
    DbSet<TurnoLaboral> TurnosLaborales { get; set; }
    DbSet<TematicaSitio> TematicasSitio { get; set; }
    DbSet<PlantillaCss> PlantillasCss { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
