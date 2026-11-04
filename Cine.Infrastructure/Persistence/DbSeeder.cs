using System.Diagnostics.CodeAnalysis;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Infrastructure.Persistence;

/// <summary>
/// Datos base requeridos por la operación de taquilla y dulcería POS (HU-06 / HU-07).
/// La siembra es idempotente: solo agrega registros faltantes sin sobrescribir ventas previas.
/// </summary>
[ExcludeFromCodeCoverage]
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

    public static async Task SeedButacasAsync(CineDbContext db)
    {
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
