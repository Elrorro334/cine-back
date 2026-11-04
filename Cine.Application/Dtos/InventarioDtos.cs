using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cine.Application.Dtos;

/// <summary>
/// DTO para la Tabla 1: Inventario Maestro de Insumos (8 columnas requeridas por HU-11)
/// </summary>
public class InsumoInventarioDto
{
    public int IdProducto { get; set; }

    /// <summary>1. SKU (Código de barras / identificador maestro)</summary>
    public string Sku { get; set; } = null!;

    /// <summary>2. Producto (Nombre comercial del insumo)</summary>
    public string Producto { get; set; } = null!;

    /// <summary>3. Categoría (Snacks, Bebidas, Dulces, Comida)</summary>
    public string Categoria { get; set; } = null!;

    /// <summary>4. Stock Actual disponible</summary>
    public int StockActual { get; set; }

    /// <summary>5. Mínimo de Alerta (umbral para resaltar desabasto)</summary>
    public int MinimoAlerta { get; set; }

    /// <summary>6. Costo Unitario de adquisición</summary>
    public decimal CostoUnitario { get; set; }

    /// <summary>7. Precio de Venta al público en POS</summary>
    public decimal PrecioVenta { get; set; }

    /// <summary>8. Margen (%) calculado: ((Precio - Costo) / Precio) * 100</summary>
    public decimal MargenPorcentaje { get; set; }

    /// <summary>Criterio 1: Verdadero si StockActual <= MinimoAlerta</summary>
    public bool EnAlerta { get; set; }

    /// <summary>Factor de equivalencia fraccionada (ej. 1 pizza = 8 rebanadas)</summary>
    public int FactorEquivalencia { get; set; } = 1;

    /// <summary>Clase CSS de ícono FontAwesome (sin emojis Unicode)</summary>
    public string Icono { get; set; } = "fa-boxes-stacked";
}

/// <summary>
/// DTO para la Tabla 2: Historial de Entradas de Mercancía
/// </summary>
public class EntradaHistorialDto
{
    public int IdFactura { get; set; }

    /// <summary>1. Proveedor</summary>
    public string Proveedor { get; set; } = null!;

    /// <summary>2. Factura (Folio)</summary>
    public string Factura { get; set; } = null!;

    /// <summary>3. Total Pagado</summary>
    public decimal TotalPagado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? RegistradoPor { get; set; }

    public List<EntradaHistorialLineaDto> Lineas { get; set; } = new();
}

public class EntradaHistorialLineaDto
{
    public int IdEntrada { get; set; }
    public int IdProducto { get; set; }
    public string Producto { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public int CantidadComprada { get; set; }
    public int FactorEquivalencia { get; set; }
    public int CantidadStockAgregada { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal CostoTotal { get; set; }
}

/// <summary>
/// DTO para registrar una nueva factura de mercancía (Modal Registrar Entrada)
/// </summary>
public class RegistrarEntradaDto
{
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "El folio de factura es obligatorio.")]
    [StringLength(50, ErrorMessage = "El folio no puede exceder 50 caracteres.")]
    public string FolioFactura { get; set; } = null!;

    [Range(0.01, 1000000, ErrorMessage = "El total pagado debe ser mayor a cero.")]
    public decimal TotalPagado { get; set; }

    [Required(ErrorMessage = "Debe capturar al menos un insumo en la factura.")]
    [MinLength(1, ErrorMessage = "Debe capturar al menos un insumo.")]
    public List<RegistrarEntradaLineaDto> Lineas { get; set; } = new();
}

public class RegistrarEntradaLineaDto
{
    [Required]
    public int IdProducto { get; set; }

    [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }

    [Range(0.00, 1000000, ErrorMessage = "El costo unitario no puede ser negativo.")]
    public decimal CostoUnitario { get; set; }
}

/// <summary>
/// DTO para edición de Costo Unitario y Precio de Venta (Criterio 3)
/// </summary>
public class ActualizarPreciosDto
{
    [Range(0.00, 1000000, ErrorMessage = "El costo unitario no puede ser negativo.")]
    public decimal CostoUnitario { get; set; }

    [Range(0.00, 1000000, ErrorMessage = "El precio de venta no puede ser negativo.")]
    public decimal PrecioVenta { get; set; }
}

/// <summary>
/// DTO de Proveedor para catálogo y modales
/// </summary>
public class ProveedorDto
{
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Rfc { get; set; }
    public string? Telefono { get; set; }
    public string? Contacto { get; set; }
}
