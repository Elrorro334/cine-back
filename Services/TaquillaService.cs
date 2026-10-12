using cine_back.Data;
using cine_back.Dtos;
using cine_back.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace cine_back.Services;

public class TaquillaService : ITaquillaService
{
    private readonly CineDbContext _db;

    public TaquillaService(CineDbContext context)
    {
        _db = context;
    }

    public async Task<List<PeliculaCarteleraDto>> ObtenerCarteleraHoyAsync(string? busqueda = null)
    {
        // 1. Obtenemos la fecha actual para cumplir la regla de negocio
        var fechaHoy = DateTime.Today;

        // 2. Iniciamos la consulta partiendo de las películas
        var query = _db.Peliculas
            // REGLA: Solo traer películas que tengan al menos una función programada para el día de HOY
            .Where(p => p.Funcions.Any(f => f.FechaFuncion.Date == fechaHoy));

        // 3. Filtro opcional: Si el cajero escribió algo en el buscador, filtramos por título
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            // Contains() se traduce a un "LIKE %busqueda%" muy eficiente en SQL
            query = query.Where(p => p.Titulo.Contains(busqueda));
        }

        // 4. Proyección a DTO (Select): SQL Server arma el JSON exacto sin cargar entidades innecesarias
        var cartelera = await query.Select(p => new PeliculaCarteleraDto
        {
            IdPelicula = p.IdPelicula,
            Titulo = p.Titulo,
            Clasificacion = p.Clasificacion,
            Director = p.Director,
            poster_image = p.PosterImage,

            // Filtramos las funciones anidadas para que solo mande las de hoy y ordenadas por hora
            Funciones = p.Funcions
                .Where(f => f.FechaFuncion.Date == fechaHoy)
                .OrderBy(f => f.HoraInicio)
                .Select(f => new FuncionHorarioDto
                {
                    IdFuncion = f.IdFuncion,
                    // Si tu campo en base de datos es TimeSpan, ToString() lo dejará como "15:00:00"
                    // Si usaste DateTime, puedes usar f.Hora.ToString("HH:mm")
                    Hora = f.HoraInicio.ToString(),
                    NombreSala = f.IdSalaNavigation.NombreSala
                })
                .ToList()
        }).ToListAsync();

        return cartelera;
    }
}