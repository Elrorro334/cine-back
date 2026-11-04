using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Services;

public class InventarioService : IInventarioService
{
    private readonly ICineDbContext _context;

    public InventarioService(ICineDbContext context)
    {
        _context = context;
    }

    public async Task<List<InsumoInventarioDto>> ObtenerInventarioMaestroAsync(string? categoria = null, string? busqueda = null)
    {
        var query = _context.Productos
            .Include(p => p.IdCategoriaNavigation)
            .Where(p => p.Activo);

        var productos = await query.ToListAsync();

        // 1. Filtrar exclusiones (Taquilla y Combos) y aplicar mapeo oficial acordado
        var resultado = new List<InsumoInventarioDto>();

        foreach (var p in productos)
        {
            var nombreCat = p.IdCategoriaNavigation?.Nombre?.Trim() ?? string.Empty;

            // Excluir Taquilla y Combos del inventario maestro de insumos
            if (nombreCat.Equals("Taquilla", StringComparison.OrdinalIgnoreCase) ||
                nombreCat.Equals("Combos", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var catMapeada = MapearCategoria(nombreCat);

            // Filtro por categoría si se especificó
            if (!string.IsNullOrWhiteSpace(categoria) &&
                !catMapeada.Equals(categoria.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Filtro por término de búsqueda (SKU o nombre)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();
                var coincideSku = p.CodigoBarras.ToLower().Contains(term);
                var coincideNombre = p.Nombre.ToLower().Contains(term);
                if (!coincideSku && !coincideNombre)
                {
                    continue;
                }
            }

            resultado.Add(MapearAInsumoDto(p, catMapeada));
        }

        return resultado.OrderBy(p => p.Categoria).ThenBy(p => p.Producto).ToList();
    }

    public async Task<InsumoInventarioDto> ObtenerInsumoPorIdAsync(int idProducto)
    {
        var p = await _context.Productos
            .Include(x => x.IdCategoriaNavigation)
            .FirstOrDefaultAsync(x => x.IdProducto == idProducto);

        if (p == null)
        {
            throw new KeyNotFoundException($"Insumo con ID {idProducto} no encontrado.");
        }

        var catMapeada = MapearCategoria(p.IdCategoriaNavigation?.Nombre);
        return MapearAInsumoDto(p, catMapeada);
    }

    public async Task<InsumoInventarioDto> ActualizarPreciosAsync(int idProducto, ActualizarPreciosDto dto)
    {
        var producto = await _context.Productos
            .Include(x => x.IdCategoriaNavigation)
            .FirstOrDefaultAsync(x => x.IdProducto == idProducto);

        if (producto == null)
        {
            throw new KeyNotFoundException($"Insumo con ID {idProducto} no encontrado.");
        }

        // Regla 2: Protección de margen negativo
        if (dto.PrecioVenta < dto.CostoUnitario)
        {
            throw new InvalidOperationException(
                $"El precio de venta (${dto.PrecioVenta:F2}) no puede ser menor al costo unitario (${dto.CostoUnitario:F2}) (Protección de margen negativo).");
        }

        producto.Costo = dto.CostoUnitario;
        producto.PrecioVenta = dto.PrecioVenta;

        await _context.SaveChangesAsync();

        var catMapeada = MapearCategoria(producto.IdCategoriaNavigation?.Nombre);
        return MapearAInsumoDto(producto, catMapeada);
    }

    public async Task<List<EntradaHistorialDto>> ObtenerHistorialEntradasAsync(int limite = 50)
    {
        var facturas = await _context.FacturasProveedor
            .Include(f => f.IdProveedorNavigation)
            .Include(f => f.IdVendedorNavigation)
            .Include(f => f.Entradas)
                .ThenInclude(e => e.IdProductoNavigation)
            .OrderByDescending(f => f.FechaRegistro)
            .Take(limite)
            .ToListAsync();

        return facturas.Select(f => new EntradaHistorialDto
        {
            IdFactura = f.IdFactura,
            Proveedor = f.IdProveedorNavigation?.Nombre ?? "Proveedor General",
            Factura = f.Folio,
            TotalPagado = f.TotalPagado,
            FechaRegistro = f.FechaRegistro,
            RegistradoPor = f.IdVendedorNavigation?.Nombre,
            Lineas = f.Entradas.Select(e => new EntradaHistorialLineaDto
            {
                IdEntrada = e.IdEntrada,
                IdProducto = e.IdProducto,
                Producto = e.IdProductoNavigation?.Nombre ?? "Insumo",
                Sku = e.IdProductoNavigation?.CodigoBarras ?? string.Empty,
                CantidadComprada = e.Cantidad,
                FactorEquivalencia = e.IdProductoNavigation?.FactorEquivalencia ?? 1,
                CantidadStockAgregada = e.CantidadStockAgregada,
                CostoUnitario = e.CostoUnitario,
                CostoTotal = e.CostoTotal
            }).ToList()
        }).ToList();
    }

    public async Task<EntradaHistorialDto> RegistrarEntradaMercanciaAsync(RegistrarEntradaDto dto, int? idVendedor = null)
    {
        var folioNormalizado = ValidarSolicitudEntrada(dto);

        var proveedor = await _context.Proveedores.FindAsync(dto.IdProveedor);
        if (proveedor == null || !proveedor.Activo)
        {
            throw new KeyNotFoundException($"Proveedor con ID {dto.IdProveedor} no existe o no está activo.");
        }

        // Validar unicidad de folio por proveedor
        var existeFolio = await _context.FacturasProveedor
            .AnyAsync(f => f.IdProveedor == dto.IdProveedor && f.Folio.ToLower() == folioNormalizado.ToLower());

        if (existeFolio)
        {
            throw new InvalidOperationException(
                $"La factura con folio '{folioNormalizado}' ya fue registrada previamente para el proveedor '{proveedor.Nombre}'.");
        }

        var productIds = dto.Lineas.Select(l => l.IdProducto).Distinct().ToList();
        var productos = await _context.Productos
            .Where(p => productIds.Contains(p.IdProducto))
            .ToDictionaryAsync(p => p.IdProducto);

        ValidarLineasFactura(dto.Lineas, productos);

        var factura = new FacturaProveedor
        {
            IdProveedor = dto.IdProveedor,
            Folio = folioNormalizado,
            TotalPagado = dto.TotalPagado,
            FechaRegistro = DateTime.UtcNow,
            IdVendedor = idVendedor
        };

        _context.FacturasProveedor.Add(factura);

        var entradasList = CrearEntradasYActualizarStock(dto.Lineas, productos, folioNormalizado, factura);

        await _context.SaveChangesAsync();

        return new EntradaHistorialDto
        {
            IdFactura = factura.IdFactura,
            Proveedor = proveedor.Nombre,
            Factura = factura.Folio,
            TotalPagado = factura.TotalPagado,
            FechaRegistro = factura.FechaRegistro,
            Lineas = entradasList.Select(e => new EntradaHistorialLineaDto
            {
                IdEntrada = e.IdEntrada,
                IdProducto = e.IdProducto,
                Producto = productos[e.IdProducto].Nombre,
                Sku = productos[e.IdProducto].CodigoBarras,
                CantidadComprada = e.Cantidad,
                FactorEquivalencia = productos[e.IdProducto].FactorEquivalencia,
                CantidadStockAgregada = e.CantidadStockAgregada,
                CostoUnitario = e.CostoUnitario,
                CostoTotal = e.CostoTotal
            }).ToList()
        };
    }

    private static string ValidarSolicitudEntrada(RegistrarEntradaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Lineas == null || dto.Lineas.Count == 0)
        {
            throw new ArgumentException("Debe capturar al menos un insumo en la factura.");
        }

        var folioNormalizado = dto.FolioFactura?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folioNormalizado))
        {
            throw new ArgumentException("El folio de factura no puede estar vacío.");
        }

        var sumaLineas = dto.Lineas.Sum(l => l.Cantidad * l.CostoUnitario);
        if (Math.Abs(dto.TotalPagado - sumaLineas) > 0.01m)
        {
            throw new InvalidOperationException(
                $"El total pagado (${dto.TotalPagado:F2}) no coincide con la suma calculada de las líneas (${sumaLineas:F2}).");
        }

        return folioNormalizado;
    }

    private static void ValidarLineasFactura(
        List<RegistrarEntradaLineaDto> lineas,
        Dictionary<int, Producto> productos)
    {
        foreach (var linea in lineas)
        {
            if (!productos.TryGetValue(linea.IdProducto, out var prod))
            {
                throw new KeyNotFoundException($"Insumo con ID {linea.IdProducto} no encontrado en el catálogo maestro.");
            }

            if (prod.PrecioVenta < linea.CostoUnitario)
            {
                throw new InvalidOperationException(
                    $"El nuevo costo unitario (${linea.CostoUnitario:F2}) de '{prod.Nombre}' excede su precio de venta actual (${prod.PrecioVenta:F2}). Actualice el precio de venta antes de ingresar esta factura para evitar margen negativo.");
            }
        }
    }

    private List<EntradaMercancium> CrearEntradasYActualizarStock(
        List<RegistrarEntradaLineaDto> lineas,
        Dictionary<int, Producto> productos,
        string folioNormalizado,
        FacturaProveedor factura)
    {
        var entradasList = new List<EntradaMercancium>();

        foreach (var linea in lineas)
        {
            var prod = productos[linea.IdProducto];
            var factor = prod.FactorEquivalencia > 0 ? prod.FactorEquivalencia : 1;
            var stockAgregado = linea.Cantidad * factor;

            prod.StockActual += stockAgregado;
            prod.Costo = linea.CostoUnitario;

            var entrada = new EntradaMercancium
            {
                IdProducto = prod.IdProducto,
                FolioFactura = folioNormalizado,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                CostoTotal = linea.Cantidad * linea.CostoUnitario,
                CantidadStockAgregada = stockAgregado,
                FechaEntrada = DateTime.UtcNow,
                IdFacturaNavigation = factura
            };

            entradasList.Add(entrada);
            _context.EntradaMercancia.Add(entrada);
        }

        return entradasList;
    }

    public async Task<List<ProveedorDto>> ObtenerProveedoresAsync()
    {
        return await _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorDto
            {
                IdProveedor = p.IdProveedor,
                Nombre = p.Nombre,
                Rfc = p.Rfc,
                Telefono = p.Telefono,
                Contacto = p.Contacto
            })
            .ToListAsync();
    }

    private static string MapearCategoria(string? categoriaOriginal)
    {
        if (string.IsNullOrWhiteSpace(categoriaOriginal)) return "Snacks";

        var cat = categoriaOriginal.Trim();
        if (cat.Equals("Palomitas", StringComparison.OrdinalIgnoreCase)) return "Snacks";
        if (cat.Equals("Alimentos", StringComparison.OrdinalIgnoreCase)) return "Comida";
        if (cat.Equals("Bebidas", StringComparison.OrdinalIgnoreCase)) return "Bebidas";
        if (cat.Equals("Dulces", StringComparison.OrdinalIgnoreCase)) return "Dulces";

        return cat;
    }

    private static InsumoInventarioDto MapearAInsumoDto(Producto p, string catMapeada)
    {
        decimal margen = 0m;
        if (p.PrecioVenta > 0)
        {
            margen = Math.Round(((p.PrecioVenta - p.Costo) / p.PrecioVenta) * 100m, 2);
        }

        // Criterio 1: Alerta si StockActual <= StockMinimo
        bool enAlerta = p.StockActual <= p.StockMinimo;

        string icono = catMapeada switch
        {
            "Snacks" => "fa-popcorn",
            "Bebidas" => "fa-glass-water",
            "Dulces" => "fa-candy-cane",
            "Comida" => "fa-burger",
            _ => "fa-boxes-stacked"
        };

        return new InsumoInventarioDto
        {
            IdProducto = p.IdProducto,
            Sku = p.CodigoBarras,
            Producto = p.Nombre,
            Categoria = catMapeada,
            StockActual = p.StockActual,
            MinimoAlerta = p.StockMinimo,
            CostoUnitario = p.Costo,
            PrecioVenta = p.PrecioVenta,
            MargenPorcentaje = margen,
            EnAlerta = enAlerta,
            FactorEquivalencia = p.FactorEquivalencia,
            Icono = icono
        };
    }
}
