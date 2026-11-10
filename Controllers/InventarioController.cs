using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

[Authorize(Roles = "Administrador,Admin")]
[ApiController]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _inventarioService;

    public InventarioController(IInventarioService inventarioService)
    {
        _inventarioService = inventarioService;
    }

    /// <summary>
    /// Tabla 1: Obtiene el Inventario Maestro de Insumos con las 8 columnas requeridas y alertas de desabasto.
    /// Excluye Taquilla y Combos; mapea categorías a Snacks, Bebidas, Dulces y Comida.
    /// </summary>
    [HttpGet("insumos")]
    public async Task<ActionResult<List<InsumoInventarioDto>>> GetInsumos(
        [FromQuery] string? categoria = null,
        [FromQuery] string? busqueda = null)
    {
        var resultado = await _inventarioService.ObtenerInventarioMaestroAsync(categoria, busqueda);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene los detalles de un insumo específico por su ID.
    /// </summary>
    [HttpGet("insumos/{id:int}")]
    public async Task<ActionResult<InsumoInventarioDto>> GetInsumoPorId(int id)
    {
        try
        {
            var resultado = await _inventarioService.ObtenerInsumoPorIdAsync(id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Criterio 3: Actualiza Costo Unitario y/o Precio de Venta, recalculando instantáneamente el margen (%).
    /// Aplica Regla 2: Protección de margen negativo.
    /// </summary>
    [HttpPut("insumos/{id:int}/precios")]
    public async Task<ActionResult<InsumoInventarioDto>> ActualizarPrecios(
        int id,
        [FromBody] ActualizarPreciosDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var resultado = await _inventarioService.ActualizarPreciosAsync(id, dto);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Tabla 2: Consulta el historial de facturas y entradas de mercancía registradas.
    /// </summary>
    [HttpGet("entradas")]
    public async Task<ActionResult<List<EntradaHistorialDto>>> GetHistorialEntradas(
        [FromQuery] int limite = 50)
    {
        var resultado = await _inventarioService.ObtenerHistorialEntradasAsync(limite);
        return Ok(resultado);
    }

    /// <summary>
    /// Criterio 2: Registra una nueva factura de mercancía incrementando existencias en el inventario.
    /// Aplica Regla 1 (equivalencia en stock fraccionado) y Regla 2 (protección de margen negativo).
    /// </summary>
    [HttpPost("entradas")]
    public async Task<ActionResult<EntradaHistorialDto>> RegistrarEntrada(
        [FromBody] RegistrarEntradaDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        int? idVendedor = null;
        var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out int parsedId))
        {
            idVendedor = parsedId;
        }

        try
        {
            var resultado = await _inventarioService.RegistrarEntradaMercanciaAsync(dto, idVendedor);
            return StatusCode(StatusCodes.Status201Created, resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Catálogo de proveedores activos para poblar el formulario modal de registro de entradas.
    /// </summary>
    [HttpGet("proveedores")]
    public async Task<ActionResult<List<ProveedorDto>>> GetProveedores()
    {
        var resultado = await _inventarioService.ObtenerProveedoresAsync();
        return Ok(resultado);
    }
}
