using System.Net;
using System.Net.Sockets;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Infrastructure.Persistence;

/// <summary>
/// Datos base requeridos por la operación de taquilla y dulcería POS (HU-06 / HU-07).
/// La siembra es idempotente: solo agrega registros faltantes sin sobrescribir ventas previas.
/// </summary>
public static class DbSeeder
{
    // Distribución física del mapa de butacas (Mockup 11 / HU-06).
    private static readonly Dictionary<int, (int Filas, int ButacasPorFila)> Distribuciones = new()
    {
        [1] = (6, 8), // Sala 1: A1..F8 = 48 butacas
        [2] = (5, 8), // Sala 2: A1..E8 = 40 butacas
    };

    private static readonly (string Nombre, string Descripcion)[] CategoriasBase =
    {
        ("Taquilla", "Boletos de acceso general a funciones"),
        ("Snacks", "Botanas y alimentos de dulcería"),
        ("Bebidas", "Refrescos y bebidas frías"),
        ("Pizzas", "Inventario fraccionado de pizzas y rebanadas")
    };

    private static readonly (string Sku, string Nombre, string Categoria, decimal Precio, decimal Costo, int Stock, int StockMin)[] ProductosBase =
    {
        ("BOL-GEN", "Boleto General", "Taquilla", 50.00m, 0.00m, 999, 10),
        ("7501001", "Palomitas Grandes", "Snacks", 50.00m, 18.00m, 40, 10),
        ("7501002", "Refresco 600ml", "Bebidas", 35.00m, 14.00m, 50, 12),
        ("7501003", "Nachos con Queso", "Snacks", 65.00m, 25.00m, 30, 8),
        ("7501004", "Chips / Frituras", "Snacks", 25.00m, 10.00m, 35, 10),
        ("7501005", "Icee / Granizado", "Bebidas", 45.00m, 15.00m, 25, 8),
        ("7501006", "Rebanada Individual", "Pizzas", 35.00m, 12.00m, 56, 8),
        ("7501007", "Pizza Completa (8 Rebanadas)", "Pizzas", 180.00m, 85.00m, 7, 1)
    };

    /// <summary>
    /// Autoriza la IP local de desarrollo en RedesAutorizadas y crea los usuarios iniciales
    /// (admin / admin123 y Raul / 123) si no existen aún en la base de datos.
    /// </summary>
    public static async Task SeedAccesoLocalAsync(CineDbContext db)
    {
        var ipsLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "127.0.0.1",
            "::1"
        };

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    ipsLocales.Add(ip.ToString());
                }
            }
        }
        catch
        {
            // Ignorar si no se puede resolver DNS local
        }

        var redesExistentes = await db.RedesAutorizadas
            .Select(r => r.DireccionIp)
            .ToListAsync();

        foreach (var ip in ipsLocales)
        {
            if (!redesExistentes.Contains(ip, StringComparer.OrdinalIgnoreCase))
            {
                db.RedesAutorizadas.Add(new RedesAutorizada { DireccionIp = ip });
            }
        }

        await db.SaveChangesAsync();

        var rolesExistentes = await db.Rols.ToListAsync();
        var rolAdmin = rolesExistentes.FirstOrDefault(r => string.Equals(r.NombreRol, "Admin", StringComparison.OrdinalIgnoreCase));
        if (rolAdmin == null)
        {
            rolAdmin = new Rol { NombreRol = "Admin", Descripcion = "Administrador del sistema" };
            db.Rols.Add(rolAdmin);
        }

        var rolVendedor = rolesExistentes.FirstOrDefault(r => string.Equals(r.NombreRol, "Vendedor", StringComparison.OrdinalIgnoreCase));
        if (rolVendedor == null)
        {
            rolVendedor = new Rol { NombreRol = "Vendedor", Descripcion = "Cajero de taquilla y dulcería POS" };
            db.Rols.Add(rolVendedor);
        }

        await db.SaveChangesAsync();

        var vendedoresExistentes = await db.Vendedors.ToListAsync();
        if (!vendedoresExistentes.Any(v => string.Equals(v.Nombre, "Raul", StringComparison.OrdinalIgnoreCase)))
        {
            db.Vendedors.Add(new Vendedor
            {
                Nombre = "Raul",
                IdRol = rolVendedor.IdRol,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                EstadoActivo = true,
                FechaCreacion = DateTime.Now
            });
        }

        if (!vendedoresExistentes.Any(v => string.Equals(v.Nombre, "admin", StringComparison.OrdinalIgnoreCase)))
        {
            db.Vendedors.Add(new Vendedor
            {
                Nombre = "admin",
                IdRol = rolAdmin.IdRol,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                EstadoActivo = true,
                FechaCreacion = DateTime.Now
            });
        }

        await db.SaveChangesAsync();

        // Si no hay funciones programadas para el día de hoy, programar cartelera activa para hoy
        var hoy = DateTime.Today;
        if (!await db.Funcions.AnyAsync(f => f.FechaFuncion.Date == hoy))
        {
            var salaPrincipal = await db.Salas.FirstOrDefaultAsync();
            if (salaPrincipal == null)
            {
                salaPrincipal = new Sala
                {
                    NombreSala = "Sala 1 - Principal",
                    CapacidadTotal = 48,
                    TipoPantalla = "2D Tradicional"
                };
                db.Salas.Add(salaPrincipal);
                await db.SaveChangesAsync();
            }

            var peliculas = await db.Peliculas.ToListAsync();
            if (peliculas.Count == 0)
            {
                var peli1 = new Pelicula
                {
                    Titulo = "Spider-Man: Un Nuevo Día",
                    Sinopsis = "Nueva entrega en pantalla gigante en Cine Teatro Xilotzin.",
                    Duracion = 125,
                    Clasificacion = "B",
                    Director = "Sam Raimi",
                    PosterImage = "https://images.unsplash.com/photo-1635805737707-575885ab0820?q=80&w=600"
                };
                var peli2 = new Pelicula
                {
                    Titulo = "Super Mario Bros: La Película",
                    Sinopsis = "Aventura animada para toda la familia en Cine Teatro Xilotzin.",
                    Duracion = 95,
                    Clasificacion = "AA",
                    Director = "Aaron Horvath",
                    PosterImage = "https://images.unsplash.com/photo-1563089145-599997674d42?q=80&w=600"
                };
                db.Peliculas.AddRange(peli1, peli2);
                await db.SaveChangesAsync();
                peliculas = await db.Peliculas.ToListAsync();
            }

            var horariosBase = new[] { new TimeSpan(15, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(20, 30, 0) };
            foreach (var peli in peliculas.Take(4))
            {
                foreach (var h in horariosBase)
                {
                    db.Funcions.Add(new Funcion
                    {
                        IdPelicula = peli.IdPelicula,
                        IdSala = salaPrincipal.IdSala,
                        FechaFuncion = hoy,
                        HoraInicio = hoy.Add(h),
                        Precio = 50.00m
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }

    public static async Task SeedButacasAsync(CineDbContext db)
    {
        if (!await db.Salas.AnyAsync())
        {
            db.Salas.Add(new Sala
            {
                NombreSala = "Sala 1 - Principal",
                CapacidadTotal = 48,
                TipoPantalla = "2D Tradicional"
            });
            await db.SaveChangesAsync();
        }

        var salas = (await db.Salas.ToListAsync())
            .Where(s => Distribuciones.ContainsKey(s.IdSala))
            .ToList();

        foreach (var sala in salas)
        {
            var (filas, butacasPorFila) = Distribuciones[sala.IdSala];

            var existentes = await db.Butacas
                .Where(b => b.IdSala == sala.IdSala)
                .Select(b => new { b.Fila, b.Numero })
                .ToListAsync();

            var codigosExistentes = existentes
                .Select(b => $"{b.Fila}{b.Numero}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (var fila = 'A'; fila < 'A' + filas; fila++)
            {
                for (var numero = 1; numero <= butacasPorFila; numero++)
                {
                    var codigo = $"{fila}{numero}";
                    if (codigosExistentes.Contains(codigo)) continue;

                    db.Butacas.Add(new Butaca
                    {
                        IdSala = sala.IdSala,
                        Fila = fila.ToString(),
                        Numero = numero,
                    });
                }
            }

            var capacidadEsperada = filas * butacasPorFila;
            if (sala.CapacidadTotal != capacidadEsperada)
            {
                sala.CapacidadTotal = capacidadEsperada;
            }
        }

        await db.SaveChangesAsync();
    }

    public static async Task SeedDulceriaAsync(CineDbContext db)
    {
        var categoriasExistentes = await db.Categoria.ToListAsync();
        foreach (var (nombre, descripcion) in CategoriasBase)
        {
            if (categoriasExistentes.Any(c => string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var nuevaCat = new Categorium
            {
                Nombre = nombre,
                Descripcion = descripcion
            };
            db.Categoria.Add(nuevaCat);
            categoriasExistentes.Add(nuevaCat);
        }

        await db.SaveChangesAsync();

        var mapaCategorias = (await db.Categoria.ToListAsync())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
            .GroupBy(c => c.Nombre!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().IdCategoria, StringComparer.OrdinalIgnoreCase);

        var productosExistentes = await db.Productos.ToListAsync();
        foreach (var item in ProductosBase)
        {
            if (productosExistentes.Any(p => string.Equals(p.CodigoBarras, item.Sku, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!mapaCategorias.TryGetValue(item.Categoria, out var idCategoria))
            {
                idCategoria = mapaCategorias.Values.FirstOrDefault(1);
            }

            db.Productos.Add(new Producto
            {
                CodigoBarras = item.Sku,
                Nombre = item.Nombre,
                IdCategoria = idCategoria,
                PrecioVenta = item.Precio,
                Costo = item.Costo,
                StockActual = item.Stock,
                StockMinimo = item.StockMin
            });
        }

        await db.SaveChangesAsync();

        var existeFraccionado = await db.InventarioStockFraccionados.AnyAsync();
        if (!existeFraccionado)
        {
            var prodRebanada = await db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501006");
            var prodPizza = await db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501007");

            if (prodRebanada != null && prodPizza != null)
            {
                var rebanadasActuales = prodRebanada.StockActual > 0 ? prodRebanada.StockActual : 56;
                db.InventarioStockFraccionados.Add(new InventarioStockFraccionado
                {
                    IdProductoMaestro = prodPizza.IdProducto,
                    IdProductoFraccion = prodRebanada.IdProducto,
                    FactorFraccion = 8,
                    StockFraccionesActual = rebanadasActuales,
                    StockFraccionesMinimo = 16,
                    UnidadMedidaFraccion = "rebanadas",
                    UnidadMedidaMaestra = "pizzas",
                    UltimaActualizacion = DateTime.Now
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
