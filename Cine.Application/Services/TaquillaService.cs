using System.Text.Json;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cine.Application.Services;

public class TaquillaService : ITaquillaService
{
    private readonly ICineDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly string _tmdbApiKey;

    public TaquillaService(ICineDbContext context, HttpClient? httpClient = null, IConfiguration? config = null)
    {
        _db = context;
        _httpClient = httpClient ?? new HttpClient();
        _tmdbApiKey = config?["Tmdb:ApiKey"] ?? "a9b90184f1235d3b78c24e4b31e993b8";
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
            Sinopsis = p.Sinopsis ?? string.Empty,
            Duracion = p.Duracion,
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

    public async Task<List<TmdbSearchResultDto>> BuscarTmdbAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<TmdbSearchResultDto>();

        try
        {
            var url = $"https://api.themoviedb.org/3/search/movie?api_key={_tmdbApiKey}&language=es-MX&query={Uri.EscapeDataString(query.Trim())}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<TmdbSearchResultDto>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var resultados = new List<TmdbSearchResultDto>();
            if (doc.RootElement.TryGetProperty("results", out var resultsElement) && resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in resultsElement.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
                    var titulo = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                    var original = item.TryGetProperty("original_title", out var origProp) ? origProp.GetString() ?? "" : "";
                    var overview = item.TryGetProperty("overview", out var overProp) ? overProp.GetString() ?? "" : "";
                    var poster = item.TryGetProperty("poster_path", out var posterProp) ? posterProp.GetString() ?? "" : "";
                    var release = item.TryGetProperty("release_date", out var relProp) ? relProp.GetString() ?? "" : "";
                    var rating = item.TryGetProperty("vote_average", out var voteProp) ? voteProp.GetDouble() : 0.0;

                    var year = !string.IsNullOrEmpty(release) && release.Length >= 4 ? release.Substring(0, 4) : "";

                    resultados.Add(new TmdbSearchResultDto
                    {
                        TmdbId = id,
                        Titulo = titulo,
                        TituloOriginal = original,
                        Sinopsis = overview,
                        PosterPath = poster, // Relativo estricto ej: "/abc.jpg"
                        ReleaseYear = year,
                        Rating = Math.Round(rating, 1)
                    });
                }
            }

            return resultados;
        }
        catch
        {
            return new List<TmdbSearchResultDto>();
        }
    }

    public async Task<TmdbMovieDetailDto?> ObtenerDetalleTmdbAsync(int tmdbId)
    {
        if (tmdbId <= 0) return null;

        try
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={_tmdbApiKey}&language=es-MX&append_to_response=credits,release_dates,videos";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : tmdbId;
            var titulo = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
            var original = root.TryGetProperty("original_title", out var origProp) ? origProp.GetString() ?? "" : "";
            var overview = root.TryGetProperty("overview", out var overProp) ? overProp.GetString() ?? "" : "";
            var runtime = root.TryGetProperty("runtime", out var runProp) && runProp.ValueKind == JsonValueKind.Number ? runProp.GetInt32() : 120;
            var poster = root.TryGetProperty("poster_path", out var posterProp) ? posterProp.GetString() ?? "" : "";
            var backdrop = root.TryGetProperty("backdrop_path", out var backProp) ? backProp.GetString() ?? "" : "";
            var release = root.TryGetProperty("release_date", out var relProp) ? relProp.GetString() ?? "" : "";
            var rating = root.TryGetProperty("vote_average", out var voteProp) ? voteProp.GetDouble() : 8.5;

            var year = !string.IsNullOrEmpty(release) && release.Length >= 4 ? release.Substring(0, 4) : DateTime.Now.Year.ToString();

            // Géneros
            var generosList = new List<string>();
            if (root.TryGetProperty("genres", out var genresProp) && genresProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var g in genresProp.EnumerateArray())
                {
                    if (g.TryGetProperty("name", out var gName))
                    {
                        var name = gName.GetString();
                        if (!string.IsNullOrEmpty(name)) generosList.Add(name);
                    }
                }
            }
            var genero = generosList.Count > 0 ? string.Join(", ", generosList) : "Estreno";

            // Créditos: Director y Actores
            var director = "Director Oficial";
            var actoresList = new List<string>();
            if (root.TryGetProperty("credits", out var creditsProp))
            {
                if (creditsProp.TryGetProperty("crew", out var crewProp) && crewProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var member in crewProp.EnumerateArray())
                    {
                        if (member.TryGetProperty("job", out var jobProp) && jobProp.GetString() == "Director")
                        {
                            director = member.TryGetProperty("name", out var dName) ? dName.GetString() ?? director : director;
                            break;
                        }
                    }
                }

                if (creditsProp.TryGetProperty("cast", out var castProp) && castProp.ValueKind == JsonValueKind.Array)
                {
                    int count = 0;
                    foreach (var actor in castProp.EnumerateArray())
                    {
                        if (count >= 4) break;
                        if (actor.TryGetProperty("name", out var aName))
                        {
                            var name = aName.GetString();
                            if (!string.IsNullOrEmpty(name))
                            {
                                actoresList.Add(name);
                                count++;
                            }
                        }
                    }
                }
            }

            // Certificación / Clasificación
            var clasificacion = "B";
            if (root.TryGetProperty("release_dates", out var rdProp) && rdProp.TryGetProperty("results", out var rdResults) && rdResults.ValueKind == JsonValueKind.Array)
            {
                foreach (var country in rdResults.EnumerateArray())
                {
                    var iso = country.TryGetProperty("iso_3166_1", out var isoProp) ? isoProp.GetString() : "";
                    if (iso == "MX" || (iso == "US" && clasificacion == "B"))
                    {
                        if (country.TryGetProperty("release_dates", out var certDates) && certDates.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var cd in certDates.EnumerateArray())
                            {
                                var cert = cd.TryGetProperty("certification", out var certProp) ? certProp.GetString() : "";
                                if (!string.IsNullOrEmpty(cert))
                                {
                                    clasificacion = cert;
                                    if (iso == "MX") break;
                                }
                            }
                        }
                    }
                }
            }

            // Tráiler Key de YouTube
            string? trailerKey = null;
            if (root.TryGetProperty("videos", out var videosProp) && videosProp.TryGetProperty("results", out var vidResults) && vidResults.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in vidResults.EnumerateArray())
                {
                    var site = v.TryGetProperty("site", out var siteProp) ? siteProp.GetString() : "";
                    var type = v.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "";
                    if (site == "YouTube" && (type == "Trailer" || trailerKey == null))
                    {
                        trailerKey = v.TryGetProperty("key", out var keyProp) ? keyProp.GetString() : null;
                        if (type == "Trailer") break;
                    }
                }
            }

            return new TmdbMovieDetailDto
            {
                TmdbId = id,
                Titulo = titulo,
                TituloOriginal = original,
                Sinopsis = overview,
                Duracion = runtime > 0 ? runtime : 120,
                Clasificacion = clasificacion,
                Director = director,
                Actores = actoresList.Count > 0 ? string.Join(", ", actoresList) : "Reparto estelar",
                Genero = genero,
                PosterPath = poster, // Relativo estricto
                BackdropPath = backdrop,
                ReleaseYear = year,
                Rating = Math.Round(rating, 1),
                TrailerKey = trailerKey
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<PeliculaCarteleraDto> RegistrarPeliculaDesdeTmdbAsync(RegistrarPeliculaDto dto)
    {
        // 1. Regla de persistencia: PROHIBIDO almacenar URLs absolutas en SQL Server.
        // Solo ruta relativa (ej: "/mo7teil1qH0SxgLijnqeYP1Eb4w.jpg").
        var posterRelativo = dto.PosterPath ?? string.Empty;
        if (posterRelativo.Contains("/p/"))
        {
            posterRelativo = posterRelativo.Substring(posterRelativo.LastIndexOf('/'));
        }
        else if (posterRelativo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(posterRelativo);
                posterRelativo = uri.AbsolutePath;
            }
            catch
            {
                posterRelativo = string.Empty;
            }
        }

        // 2. Normalización de sinopsis respetando VARCHAR(100) en BD
        var sinopsisNormalizada = dto.Sinopsis?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sinopsisNormalizada))
        {
            sinopsisNormalizada = "Sinopsis oficial en Cine Teatro Xilotzin.";
        }
        if (sinopsisNormalizada.Length > 100)
        {
            sinopsisNormalizada = sinopsisNormalizada.Substring(0, 97) + "...";
        }

        // 3. Validar existencia de sala o crear Sala por defecto
        var sala = await _db.Salas.FirstOrDefaultAsync(s => dto.IdSala.HasValue && s.IdSala == dto.IdSala.Value)
                   ?? await _db.Salas.FirstOrDefaultAsync();

        if (sala == null)
        {
            sala = new Sala
            {
                NombreSala = "Sala 1 - Principal",
                CapacidadTotal = 100,
                TipoPantalla = "MacroXE"
            };
            _db.Salas.Add(sala);
            await _db.SaveChangesAsync();
        }

        // 4. Buscar si la película ya existe para evitar duplicados en la base de datos
        var tituloLimpio = dto.Titulo.Trim();
        var pelicula = await _db.Peliculas
            .FirstOrDefaultAsync(p => p.Titulo.ToLower() == tituloLimpio.ToLower());

        if (pelicula == null)
        {
            pelicula = new Pelicula
            {
                Titulo = tituloLimpio,
                Sinopsis = sinopsisNormalizada,
                Duracion = dto.Duracion > 0 ? dto.Duracion : 120,
                Clasificacion = string.IsNullOrWhiteSpace(dto.Clasificacion) ? "B" : dto.Clasificacion.Trim(),
                Director = string.IsNullOrWhiteSpace(dto.Director) ? "Director Oficial" : dto.Director.Trim(),
                PosterImage = posterRelativo
            };
            _db.Peliculas.Add(pelicula);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Si ya existía, actualizar poster y sinopsis si es necesario
            if (!string.IsNullOrEmpty(posterRelativo))
            {
                pelicula.PosterImage = posterRelativo;
            }
            pelicula.Sinopsis = sinopsisNormalizada;
            await _db.SaveChangesAsync();
        }

        // 5. Crear Función para el día de hoy evitando duplicar el mismo horario
        var fechaHoy = DateTime.Today;
        var horaInicio = fechaHoy.AddHours(16); // Default 16:00
        if (!string.IsNullOrWhiteSpace(dto.Horario) && TimeSpan.TryParse(dto.Horario, out var timeSpan))
        {
            horaInicio = fechaHoy.Add(timeSpan);
        }

        var funcion = await _db.Funcions.FirstOrDefaultAsync(f =>
            f.IdPelicula == pelicula.IdPelicula &&
            f.IdSala == sala.IdSala &&
            f.FechaFuncion.Date == fechaHoy &&
            f.HoraInicio == horaInicio);

        if (funcion == null)
        {
            funcion = new Funcion
            {
                IdPelicula = pelicula.IdPelicula,
                IdSala = sala.IdSala,
                FechaFuncion = fechaHoy,
                HoraInicio = horaInicio,
                Precio = dto.Precio > 0 ? dto.Precio : 50.00m
            };
            _db.Funcions.Add(funcion);
            await _db.SaveChangesAsync();
        }

        return new PeliculaCarteleraDto
        {
            IdPelicula = pelicula.IdPelicula,
            Titulo = pelicula.Titulo,
            Sinopsis = pelicula.Sinopsis,
            Duracion = pelicula.Duracion,
            Clasificacion = pelicula.Clasificacion,
            Director = pelicula.Director,
            poster_image = pelicula.PosterImage ?? string.Empty,
            Funciones = new List<FuncionHorarioDto>
            {
                new FuncionHorarioDto
                {
                    IdFuncion = funcion.IdFuncion,
                    Hora = funcion.HoraInicio.ToString(),
                    NombreSala = sala.NombreSala
                }
            }
        };
    }

    public async Task<bool> EliminarPeliculaAsync(int idPelicula)
    {
        var pelicula = await _db.Peliculas
            .Include(p => p.Funcions)
            .FirstOrDefaultAsync(p => p.IdPelicula == idPelicula);

        if (pelicula == null) return false;

        var idsFunciones = pelicula.Funcions.Select(f => f.IdFuncion).ToList();
        if (idsFunciones.Count > 0)
        {
            var estados = await _db.EstadoButacaFuncions
                .Where(e => idsFunciones.Contains(e.IdFuncion))
                .ToListAsync();
            _db.EstadoButacaFuncions.RemoveRange(estados);

            var preordenes = await _db.Preordens
                .Where(pr => idsFunciones.Contains(pr.IdFuncion))
                .ToListAsync();

            if (preordenes.Count > 0)
            {
                var idsPreorden = preordenes.Select(pr => pr.IdPreorden).ToList();
                var detalles = await _db.DetallePreordenBoletos
                    .Where(d => idsPreorden.Contains(d.IdPreorden))
                    .ToListAsync();
                _db.DetallePreordenBoletos.RemoveRange(detalles);

                _db.Preordens.RemoveRange(preordenes);
            }

            _db.Funcions.RemoveRange(pelicula.Funcions);
        }

        _db.Peliculas.Remove(pelicula);
        await _db.SaveChangesAsync();
        return true;
    }
}