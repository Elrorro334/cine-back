
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Services;

public class TaquillaService : ITaquillaService
{
    private readonly ICineDbContext _db;

    public TaquillaService(ICineDbContext context)
    {
        _db = context;
    }

    public async Task<List<PeliculaCarteleraDto>> ObtenerCarteleraHoyAsync(string? busqueda = null)
    {
        var fechaHoy = DateTime.Today;

        var query = _db.Peliculas
            .Where(p => p.Funcions.Any(f => f.FechaFuncion.Date == fechaHoy));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(p => p.Titulo.Contains(busqueda));
        }

        var cartelera = await query.Select(p => new PeliculaCarteleraDto
        {
            IdPelicula = p.IdPelicula,
            Titulo = p.Titulo,
            Clasificacion = p.Clasificacion ?? string.Empty,
            Director = p.Director ?? string.Empty,
            poster_image = p.PosterImage ?? string.Empty,

            Funciones = p.Funcions
                .Where(f => f.FechaFuncion.Date == fechaHoy)
                .OrderBy(f => f.HoraInicio)
                .Select(f => new FuncionHorarioDto
                {
                    IdFuncion = f.IdFuncion,
                    Hora = f.HoraInicio.ToString(),
                    NombreSala = f.IdSalaNavigation != null ? f.IdSalaNavigation.NombreSala : string.Empty
                })
                .ToList()
        }).ToListAsync();

        return cartelera;
    }
}