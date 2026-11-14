using System.Collections.Generic;
using System.Threading.Tasks;
using Cine.Application.Dtos;

namespace Cine.Application.Interfaces;

public interface IInventarioService
{
    /// <summary>
    /// Criterio 1: Obtiene el inventario maestro con las 8 columnas requeridas y estado de alerta de desabasto.
    /// Excluye categorías 'Taquilla' y 'Combos', mapeando a Snacks, Bebidas, Dulces y Comida.
    /// </summary>
    Task<List<InsumoInventarioDto>> ObtenerInventarioMaestroAsync(string? categoria = null, string? busqueda = null);

    /// <summary>
    /// Obtiene el inventario maestro paginado (15 registros por página por defecto),
    /// con búsqueda acento-insensible, filtro de estado de stock, ordenamiento y contadores reales.
    /// </summary>
    Task<InventarioPaginadoDto> ObtenerInventarioPaginadoAsync(InventarioFiltroDto filtro);

    /// <summary>
    /// Obtiene un insumo individual con su margen calculado.
    /// </summary>
    Task<InsumoInventarioDto> ObtenerInsumoPorIdAsync(int idProducto);

    /// <summary>
    /// Criterio 3: Actualiza Costo Unitario y/o Precio de Venta, recalculando instantáneamente el margen.
    /// Aplica Regla 2: Protección de margen negativo (PrecioVenta >= Costo).
    /// </summary>
    Task<InsumoInventarioDto> ActualizarPreciosAsync(int idProducto, ActualizarPreciosDto dto);

    /// <summary>
    /// Tabla 2: Consulta el historial de facturas y entradas de mercancía registradas.
    /// </summary>
    Task<List<EntradaHistorialDto>> ObtenerHistorialEntradasAsync(int limite = 50);

    /// <summary>
    /// Criterio 2: Registra una factura de mercancía incrementando el stock físico.
    /// Aplica Regla 1: Multiplicación por factor de equivalencia (stock fraccionado).
    /// Aplica Regla 2: Protección de margen negativo ante nuevo costo unitario.
    /// Actualiza el costo unitario con el último costo.
    /// </summary>
    Task<EntradaHistorialDto> RegistrarEntradaMercanciaAsync(RegistrarEntradaDto dto, int? idVendedor = null);

    /// <summary>
    /// Catálogo de proveedores activos para el formulario de entrada.
    /// </summary>
    Task<List<ProveedorDto>> ObtenerProveedoresAsync();
}
