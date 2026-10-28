using System.Globalization;
using System.Text.Json;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cine.Application.Services;

public class TaquillaService : ITaquillaService
{
    private static readonly Dictionary<string, string> MapaCertificaciones = new(StringComparer.OrdinalIgnoreCase)
    {
        { "R", "C" },
        { "PG-13", "B15" },
        { "PG", "B" },
        { "G", "AA" },
        { "NC-17", "C" },
        { "A", "A" },
        { "AA", "AA" },
        { "B", "B" },
        { "B15", "B15" },
        { "C", "C" }
    };

    private static readonly Dictionary<string, string> ImagenesDulceria = new()
    {
        { "7501001", "https://images.unsplash.com/photo-1585647347384-2593bc35786b?q=80&w=400&auto=format&fit=crop" },
        { "7501002", "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?q=80&w=400&auto=format&fit=crop" },
        { "7501003", "https://images.unsplash.com/photo-1513456852971-30c0b8199d4d?q=80&w=400&auto=format&fit=crop" },
        { "7501004", "https://images.unsplash.com/photo-1566478989037-e924e50cb792?q=80&w=400&auto=format&fit=crop" },
        { "7501005", "https://images.pexels.com/photos/29269189/pexels-photo-29269189.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=400" },
        { "7501006", "https://images.unsplash.com/photo-1513104890138-7c749659a591?q=80&w=400&auto=format&fit=crop" },
        { "7501007", "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?q=80&w=400&auto=format&fit=crop" },
        { "COMBO01", "https://images.unsplash.com/photo-1585647347384-2593bc35786b?q=80&w=400" },
        { "COMBO02", "https://images.unsplash.com/photo-1513456852971-30c0b8199d4d?q=80&w=400" },
        { "COMBO03", "https://images.unsplash.com/photo-1513104890138-7c749659a591?q=80&w=400" }
    };

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
                    NombreSala = f.IdSalaNavigation != null ? f.IdSalaNavigation.NombreSala : string.Empty,
                    Precio = f.Precio
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
                    resultados.Add(MapearResultadoBusquedaTmdb(item));
                }
            }

            return resultados;
        }
        catch
        {
            return new List<TmdbSearchResultDto>();
        }
    }

    private static TmdbSearchResultDto MapearResultadoBusquedaTmdb(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
        var titulo = GetJsonString(item, "title");
        var original = GetJsonString(item, "original_title");
        var overview = GetJsonString(item, "overview");
        var poster = GetJsonString(item, "poster_path");
        var release = GetJsonString(item, "release_date");
        var rating = item.TryGetProperty("vote_average", out var voteProp) ? voteProp.GetDouble() : 0.0;
        var year = release.Length >= 4 ? release.Substring(0, 4) : "";

        return new TmdbSearchResultDto
        {
            TmdbId = id,
            Titulo = titulo,
            TituloOriginal = original,
            Sinopsis = overview,
            PosterPath = poster,
            ReleaseYear = year,
            Rating = Math.Round(rating, 1)
        };
    }

    public async Task<TmdbMovieDetailDto?> ObtenerDetalleTmdbAsync(int tmdbId)
    {
        if (tmdbId <= 0) return null;

        try
        {
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={_tmdbApiKey}&language=es-MX&append_to_response=release_dates,credits,videos";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return ConstruirDetalleTmdb(doc.RootElement, tmdbId);
        }
        catch
        {
            return null;
        }
    }

    private static TmdbMovieDetailDto ConstruirDetalleTmdb(JsonElement root, int fallbackId)
    {
        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : fallbackId;
        var titulo = GetJsonString(root, "title");
        var original = GetJsonString(root, "original_title");
        var tagline = GetJsonString(root, "tagline");
        var eslogan = !string.IsNullOrWhiteSpace(tagline) ? tagline : "Cine Tradicional en Jilotepec";

        var overview = GetJsonString(root, "overview");
        var sinopsis = !string.IsNullOrWhiteSpace(overview)
            ? overview
            : "Disfruta de esta emocionante producción en nuestra pantalla gigante con audio cinematográfico inmersivo.";

        var runtime = root.TryGetProperty("runtime", out var runProp) && runProp.ValueKind == JsonValueKind.Number ? runProp.GetInt32() : 120;
        var poster = GetJsonString(root, "poster_path");
        var backdrop = GetJsonString(root, "backdrop_path");
        var release = GetJsonString(root, "release_date");
        var voteAvg = root.TryGetProperty("vote_average", out var voteProp) && voteProp.ValueKind == JsonValueKind.Number ? voteProp.GetDouble() : 8.5;
        var year = release.Length >= 4 ? release.Substring(0, 4) : DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);

        var (genero, generosCompletos) = ExtraerGeneros(root);
        var (director, actores) = ExtraerCreditos(root);
        var clasificacion = ExtraerClasificacion(root);
        var trailerKey = ExtraerTrailerKey(root);

        return new TmdbMovieDetailDto
        {
            TmdbId = id,
            Titulo = !string.IsNullOrWhiteSpace(titulo) ? titulo : original,
            TituloOriginal = original,
            Eslogan = eslogan,
            Sinopsis = sinopsis,
            Duracion = runtime > 0 ? runtime : 120,
            Clasificacion = clasificacion,
            Director = director,
            Actores = actores,
            Genero = genero,
            GenerosCompletos = generosCompletos,
            PosterPath = poster,
            BackdropPath = backdrop,
            ReleaseYear = year,
            Rating = Math.Round(voteAvg, 1),
            TrailerKey = trailerKey
        };
    }

    private static string GetJsonString(JsonElement element, string propertyName, string fallback = "")
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? fallback;
        }
        return fallback;
    }

    private static (string GeneroPrincipal, string GenerosCompletos) ExtraerGeneros(JsonElement root)
    {
        var generosList = new List<string>();
        if (root.TryGetProperty("genres", out var genresProp) && genresProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in genresProp.EnumerateArray())
            {
                var name = GetJsonString(g, "name");
                if (!string.IsNullOrEmpty(name)) generosList.Add(name);
            }
        }

        var genero = generosList.Count > 0 ? generosList[0] : "Cine";
        var generosCompletos = generosList.Count > 0 ? string.Join(", ", generosList) : "Acción, Aventuras";
        return (genero, generosCompletos);
    }

    private static (string Director, string Actores) ExtraerCreditos(JsonElement root)
    {
        if (!root.TryGetProperty("credits", out var creditsProp))
        {
            return ("Director Cinematográfico", "Elenco estelar");
        }

        var director = ExtraerDirector(creditsProp);
        var actores = ExtraerActores(creditsProp);
        return (director, actores);
    }

    private static string ExtraerDirector(JsonElement creditsProp)
    {
        if (creditsProp.TryGetProperty("crew", out var crewProp) && crewProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var member in crewProp.EnumerateArray())
            {
                if (GetJsonString(member, "job") == "Director")
                {
                    var name = GetJsonString(member, "name");
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
        }
        return "Director Cinematográfico";
    }

    private static string ExtraerActores(JsonElement creditsProp)
    {
        var actoresList = new List<string>();
        if (creditsProp.TryGetProperty("cast", out var castProp) && castProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var actor in castProp.EnumerateArray())
            {
                var name = GetJsonString(actor, "name");
                if (!string.IsNullOrEmpty(name))
                {
                    actoresList.Add(name);
                    if (actoresList.Count >= 3) break;
                }
            }
        }
        return actoresList.Count > 0 ? string.Join(", ", actoresList) : "Elenco estelar";
    }

    private static string ExtraerClasificacion(JsonElement root)
    {
        if (!root.TryGetProperty("release_dates", out var rdProp) ||
            !rdProp.TryGetProperty("results", out var rdResults) ||
            rdResults.ValueKind != JsonValueKind.Array)
        {
            return "B";
        }

        JsonElement? targetRelease = BuscarReleasePais(rdResults);
        if (!targetRelease.HasValue ||
            !targetRelease.Value.TryGetProperty("release_dates", out var datesArr) ||
            datesArr.ValueKind != JsonValueKind.Array)
        {
            return "B";
        }

        return ExtraerCertificacionDesdeArreglo(datesArr);
    }

    private static JsonElement? BuscarReleasePais(JsonElement rdResults)
    {
        JsonElement? mxRelease = null;
        JsonElement? usRelease = null;

        foreach (var country in rdResults.EnumerateArray())
        {
            var iso = GetJsonString(country, "iso_3166_1");
            if (iso == "MX" && mxRelease == null) mxRelease = country;
            if (iso == "US" && usRelease == null) usRelease = country;
        }

        return mxRelease ?? usRelease;
    }

    private static string ExtraerCertificacionDesdeArreglo(JsonElement datesArr)
    {
        foreach (var cd in datesArr.EnumerateArray())
        {
            var cert = GetJsonString(cd, "certification").Trim();
            if (!string.IsNullOrWhiteSpace(cert))
            {
                return MapaCertificaciones.TryGetValue(cert, out var certMapeada) ? certMapeada : cert;
            }
        }
        return "B";
    }

    private static string? ExtraerTrailerKey(JsonElement root)
    {
        if (!root.TryGetProperty("videos", out var videosProp) ||
            !videosProp.TryGetProperty("results", out var vidResults) ||
            vidResults.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? trailerKey = null;
        foreach (var v in vidResults.EnumerateArray())
        {
            var site = GetJsonString(v, "site");
            var type = GetJsonString(v, "type");
            if (site == "YouTube" && (type == "Trailer" || trailerKey == null))
            {
                var key = GetJsonString(v, "key");
                trailerKey = !string.IsNullOrEmpty(key) ? key : null;
                if (type == "Trailer") break;
            }
        }

        return trailerKey;
    }

    public async Task<PeliculaCarteleraDto> RegistrarPeliculaDesdeTmdbAsync(RegistrarPeliculaDto dto)
    {
        var posterRelativo = NormalizarPosterRelativo(dto.PosterPath);
        var sinopsisNormalizada = NormalizarSinopsis(dto.Sinopsis);
        var sala = await ObtenerOCrearSalaAsync(dto.IdSala);
        var pelicula = await ObtenerOCrearPeliculaAsync(dto, sinopsisNormalizada, posterRelativo);

        var (fechaInicio, fechaFin) = NormalizarRangoFechas(dto.FechaInicio, dto.FechaFin);
        var listaHorarios = ObtenerListaHorarios(dto);
        var precioFuncion = dto.Precio > 0 ? dto.Precio : 50.00m;

        var funcionesParaPelicula = await ProgramarFuncionesEnRangoAsync(
            pelicula.IdPelicula, sala.IdSala, precioFuncion, listaHorarios, fechaInicio, fechaFin);

        var fechaHoy = DateTime.Today;
        var funcionesRespuesta = funcionesParaPelicula
            .Where(f => f.FechaFuncion.Date == fechaHoy || f.FechaFuncion.Date == fechaInicio)
            .OrderBy(f => f.HoraInicio)
            .Select(f => new FuncionHorarioDto
            {
                IdFuncion = f.IdFuncion,
                Hora = f.HoraInicio.ToString(),
                NombreSala = sala.NombreSala,
                Precio = f.Precio
            })
            .ToList();

        return new PeliculaCarteleraDto
        {
            IdPelicula = pelicula.IdPelicula,
            Titulo = pelicula.Titulo,
            Sinopsis = pelicula.Sinopsis,
            Duracion = pelicula.Duracion,
            Clasificacion = pelicula.Clasificacion,
            Director = pelicula.Director,
            poster_image = pelicula.PosterImage ?? string.Empty,
            Funciones = funcionesRespuesta
        };
    }

    private static string NormalizarPosterRelativo(string? posterPath)
    {
        var posterRelativo = posterPath ?? string.Empty;
        if (posterRelativo.Contains("/p/"))
        {
            return posterRelativo.Substring(posterRelativo.LastIndexOf('/'));
        }

        if (posterRelativo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.TryCreate(posterRelativo, UriKind.Absolute, out var uri)
                ? uri.AbsolutePath
                : string.Empty;
        }

        return posterRelativo;
    }

    private static string NormalizarSinopsis(string? sinopsis)
    {
        var sinopsisNormalizada = sinopsis?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sinopsisNormalizada))
        {
            sinopsisNormalizada = "Sinopsis oficial en Cine Teatro Xilotzin.";
        }
        return sinopsisNormalizada.Length > 100
            ? sinopsisNormalizada.Substring(0, 97) + "..."
            : sinopsisNormalizada;
    }

    private async Task<Sala> ObtenerOCrearSalaAsync(int? idSala)
    {
        var sala = await _db.Salas.FirstOrDefaultAsync(s => idSala.HasValue && s.IdSala == idSala.Value)
                   ?? await _db.Salas.FirstOrDefaultAsync();

        if (sala != null) return sala;

        sala = new Sala
        {
            NombreSala = "Sala 1 - Principal",
            CapacidadTotal = 100,
            TipoPantalla = "MacroXE"
        };
        _db.Salas.Add(sala);
        await _db.SaveChangesAsync();
        return sala;
    }

    private async Task<Pelicula> ObtenerOCrearPeliculaAsync(RegistrarPeliculaDto dto, string sinopsisNormalizada, string posterRelativo)
    {
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
        }
        else
        {
            if (!string.IsNullOrEmpty(posterRelativo))
            {
                pelicula.PosterImage = posterRelativo;
            }
            pelicula.Sinopsis = sinopsisNormalizada;
        }

        await _db.SaveChangesAsync();
        return pelicula;
    }

    private static (DateTime Inicio, DateTime Fin) NormalizarRangoFechas(DateTime? inicioDto, DateTime? finDto)
    {
        var fechaInicio = inicioDto?.Date ?? DateTime.Today;
        var fechaFin = finDto?.Date ?? fechaInicio;
        if (fechaFin < fechaInicio) fechaFin = fechaInicio;
        if ((fechaFin - fechaInicio).TotalDays > 60) fechaFin = fechaInicio.AddDays(60);
        return (fechaInicio, fechaFin);
    }

    private static List<string> ObtenerListaHorarios(RegistrarPeliculaDto dto)
    {
        var listaHorarios = new List<string>();
        if (dto.Horarios != null && dto.Horarios.Count > 0)
        {
            listaHorarios.AddRange(dto.Horarios.Where(h => !string.IsNullOrWhiteSpace(h)));
        }
        else if (!string.IsNullOrWhiteSpace(dto.Horario))
        {
            listaHorarios.Add(dto.Horario);
        }

        if (listaHorarios.Count == 0)
        {
            listaHorarios.Add("16:00");
        }
        return listaHorarios;
    }

    private async Task<List<Funcion>> ProgramarFuncionesEnRangoAsync(
        int idPelicula, int idSala, decimal precio, List<string> listaHorarios, DateTime fechaInicio, DateTime fechaFin)
    {
        var funcionesParaPelicula = new List<Funcion>();

        for (var dia = fechaInicio; dia <= fechaFin; dia = dia.AddDays(1))
        {
            foreach (var hStr in listaHorarios)
            {
                if (!TimeSpan.TryParse(hStr, CultureInfo.InvariantCulture, out var timeSpan))
                {
                    continue;
                }

                var funcion = await ObtenerOCrearFuncionAsync(idPelicula, idSala, dia, dia.Add(timeSpan), precio);
                funcionesParaPelicula.Add(funcion);
            }
        }

        await _db.SaveChangesAsync();
        return funcionesParaPelicula;
    }

    private async Task<Funcion> ObtenerOCrearFuncionAsync(
        int idPelicula, int idSala, DateTime dia, DateTime horaInicio, decimal precio)
    {
        var funcionExistente = await _db.Funcions.FirstOrDefaultAsync(f =>
            f.IdPelicula == idPelicula &&
            f.IdSala == idSala &&
            f.FechaFuncion.Date == dia.Date &&
            f.HoraInicio == horaInicio);

        if (funcionExistente != null) return funcionExistente;

        var nuevaFuncion = new Funcion
        {
            IdPelicula = idPelicula,
            IdSala = idSala,
            FechaFuncion = dia.Date,
            HoraInicio = horaInicio,
            Precio = precio
        };
        _db.Funcions.Add(nuevaFuncion);
        return nuevaFuncion;
    }

    public async Task<List<PeliculaCarteleraDto>> SincronizarCatalogoBaseAsync()
    {
        var catalogoLocal = new List<(string Titulo, List<string> Horarios)>
        {
            ("Caminos del Crimen", new List<string> { "10:30", "15:00", "19:30" }),
            ("Payaso terrifier", new List<string> { "12:30", "17:15", "22:00" }),
            ("La posesion de la momia", new List<string> { "14:30", "19:00" }),
            ("Spiderman", new List<string> { "11:00", "16:30", "21:15" }),
            ("The Super Mario Bros. Movie", new List<string> { "13:00", "16:00", "18:45" }),
            ("Avatar: Fire and Ash", new List<string> { "15:30", "21:00" }),
            ("Five Nights at Freddy's 2", new List<string> { "17:45", "20:30", "23:15" }),
            ("Super Mario Galaxy", new List<string> { "09:00", "12:00", "14:45" })
        };

        var fallbackPosters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Caminos del Crimen", "https://images.unsplash.com/photo-1536440136628-849c177e76a1?q=80&w=600" },
            { "Payaso terrifier", "https://images.unsplash.com/photo-1509281373149-e957c6296406?q=80&w=600" },
            { "La posesion de la momia", "https://images.unsplash.com/photo-1518676590629-3dcbd9c5a5c9?q=80&w=600" },
            { "Spiderman", "https://images.unsplash.com/photo-1635805737707-575885ab0820?q=80&w=600" },
            { "The Super Mario Bros. Movie", "https://images.unsplash.com/photo-1563089145-599997674d42?q=80&w=600" },
            { "Avatar: Fire and Ash", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?q=80&w=600" },
            { "Five Nights at Freddy's 2", "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?q=80&w=600" },
            { "Super Mario Galaxy", "https://images.unsplash.com/photo-1550745165-9bc0b252726f?q=80&w=600" }
        };

        var fechaHoy = DateTime.Today;
        var fechaFin = fechaHoy.AddDays(7);
        var carteleraSincronizada = new List<PeliculaCarteleraDto>();

        foreach (var item in catalogoLocal)
        {
            var detalle = await BuscarDetallePeliculaCatalogoAsync(item.Titulo);
            var poster = detalle?.PosterPath ?? (fallbackPosters.TryGetValue(item.Titulo, out var fbPoster) ? fbPoster : "");
            var dto = new RegistrarPeliculaDto
            {
                Titulo = detalle?.Titulo ?? item.Titulo,
                Sinopsis = detalle?.Sinopsis ?? "Gran Función en Pantalla Gigante en Cine Teatro Xilotzin.",
                Duracion = detalle?.Duracion ?? 115,
                Clasificacion = detalle?.Clasificacion ?? "B",
                Director = detalle?.Director ?? "Director Cinematográfico",
                PosterPath = poster,
                Precio = 50.00m,
                Horarios = item.Horarios,
                FechaInicio = fechaHoy,
                FechaFin = fechaFin,
                IdSala = 1
            };

            var registrada = await RegistrarPeliculaDesdeTmdbAsync(dto);
            carteleraSincronizada.Add(registrada);
        }

        return carteleraSincronizada;
    }

    private async Task<TmdbMovieDetailDto?> BuscarDetallePeliculaCatalogoAsync(string titulo)
    {
        var resultados = await BuscarTmdbAsync(titulo);
        if (resultados.Count > 0)
        {
            return await ObtenerDetalleTmdbAsync(resultados[0].TmdbId);
        }
        return null;
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

    public Task<List<PromocionDto>> ObtenerPromocionesAsync()
    {
        var promociones = new List<PromocionDto>
        {
            new PromocionDto
            {
                Id = 1,
                Titulo = "¡Gran Estreno de Medianoche Este Jueves!",
                Tag = "Estreno Exclusivo",
                Descripcion = "Aparta tus boletos de acceso general en preventa. Sonido digital envolvente y pantalla gigante en Jilotepec.",
                Fecha = "Válido esta semana",
                Bg = "from-red-950/80 via-black to-red-950/80",
                Icono = "fa-clapperboard"
            },
            new PromocionDto
            {
                Id = 2,
                Titulo = "Miércoles 2x1 en Boletos de Taquilla",
                Tag = "Promoción Tradicional",
                Descripcion = "Todos los miércoles vive el séptimo arte al doble. Aplica en todas las funciones y horarios del día.",
                Fecha = "Todos los miércoles",
                Bg = "from-amber-950/80 via-black to-neutral-950",
                Icono = "fa-ticket"
            },
            new PromocionDto
            {
                Id = 3,
                Titulo = "Pre-ordena en Línea y Paga en Taquilla",
                Tag = "Nuevo Módulo QR",
                Descripcion = "Genera tu código QR con validez de 8 a 15 días. Llega a caja física, escanea tu ticket digital y evita filas.",
                Fecha = "Servicio 24/7",
                Bg = "from-emerald-950/80 via-black to-neutral-950",
                Icono = "fa-qrcode"
            }
        };

        return Task.FromResult(promociones);
    }

    public async Task<FuncionButacasDto> ObtenerButacasFuncionAsync(int idFuncion)
    {
        var funcion = await _db.Funcions
            .Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == idFuncion);

        if (funcion == null)
        {
            throw new ArgumentException($"No se encontró la función con ID {idFuncion}.");
        }

        var butacasSala = await _db.Butacas
            .Where(b => b.IdSala == funcion.IdSala)
            .OrderBy(b => b.Fila)
            .ThenBy(b => b.Numero)
            .ToListAsync();

        var ocupadasIds = await _db.EstadoButacaFuncions
            .Where(e => e.IdFuncion == idFuncion && e.Estato)
            .Select(e => e.IdButaca)
            .ToListAsync();

        var ocupadasSet = new HashSet<int>(ocupadasIds);

        var butacasDto = butacasSala.Select(b => new ButacaDto
        {
            IdButaca = b.IdButaca,
            Fila = b.Fila,
            Numero = b.Numero,
            Codigo = $"{b.Fila}{b.Numero}",
            EstaOcupada = ocupadasSet.Contains(b.IdButaca)
        }).ToList();

        return new FuncionButacasDto
        {
            IdFuncion = funcion.IdFuncion,
            IdSala = funcion.IdSala,
            NombreSala = funcion.IdSalaNavigation?.NombreSala ?? "Sala 1",
            Precio = funcion.Precio,
            Butacas = butacasDto
        };
    }

    public async Task<List<ProductoDto>> ObtenerProductosDulceriaAsync()
    {
        var productos = await _db.Productos
            .Include(p => p.IdCategoriaNavigation)
            .Where(p => p.IdCategoriaNavigation.Nombre != "Taquilla")
            .ToListAsync();

        return productos.Select(p => new ProductoDto
        {
            IdProducto = p.IdProducto,
            Sku = p.CodigoBarras,
            Nombre = p.Nombre,
            Categoria = p.IdCategoriaNavigation?.Nombre ?? "Snacks",
            Precio = p.PrecioVenta,
            Stock = p.StockActual,
            Img = ImagenesDulceria.TryGetValue(p.CodigoBarras, out var imgUrl)
                ? imgUrl
                : "https://images.unsplash.com/photo-1585647347384-2593bc35786b?q=80&w=400"
        }).ToList();
    }

    public async Task<VentaConfirmadaDto> RegistrarVentaAsync(RegistrarVentaDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("No se pueden registrar ventas sin artículos.");
        }

        var totalCalculado = dto.Items.Sum(i => i.Precio * i.Cantidad);
        var metodoNormalizado = string.IsNullOrWhiteSpace(dto.MetodoPago) ? "Efectivo" : dto.MetodoPago;

        var venta = new Ventum
        {
            IdVendedor = dto.IdVendedor > 0 ? dto.IdVendedor : 1,
            FechaVenta = DateTime.Now,
            Total = totalCalculado,
            MetodoPago = metodoNormalizado
        };

        _db.Venta.Add(venta);
        await _db.SaveChangesAsync();

        var productoBoleto = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "BOL-GEN");

        foreach (var item in dto.Items)
        {
            await ProcesarItemVentaAsync(venta.IdVenta, item, productoBoleto);
        }

        await _db.SaveChangesAsync();

        return new VentaConfirmadaDto
        {
            IdVenta = venta.IdVenta,
            Folio = $"VT-{venta.IdVenta:D5}",
            Total = venta.Total,
            MetodoPago = metodoNormalizado,
            FechaVenta = venta.FechaVenta,
            Items = dto.Items
        };
    }

    private async Task ProcesarItemVentaAsync(int idVenta, RegistrarVentaItemDto item, Producto? productoBoleto)
    {
        if (item.Tipo == "dulceria" || item.IdProducto.HasValue)
        {
            await ProcesarItemDulceriaAsync(idVenta, item);
        }
        else if (item.Tipo == "boleto")
        {
            await ProcesarItemBoletoAsync(idVenta, item, productoBoleto);
        }
    }

    private async Task ProcesarItemDulceriaAsync(int idVenta, RegistrarVentaItemDto item)
    {
        var producto = item.IdProducto.HasValue
            ? await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto.Value)
            : await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == item.Sku || p.Nombre == item.Nombre);

        if (producto == null) return;

        _db.DetalleVenta.Add(new DetalleVentum
        {
            IdVenta = idVenta,
            IdProducto = producto.IdProducto,
            Cantidad = item.Cantidad
        });

        producto.StockActual = Math.Max(0, producto.StockActual - item.Cantidad);
    }

    private async Task ProcesarItemBoletoAsync(int idVenta, RegistrarVentaItemDto item, Producto? productoBoleto)
    {
        if (productoBoleto != null)
        {
            _db.DetalleVenta.Add(new DetalleVentum
            {
                IdVenta = idVenta,
                IdProducto = productoBoleto.IdProducto,
                Cantidad = item.Cantidad
            });
        }

        if (item.IdFuncion.HasValue && item.Asientos != null && item.Asientos.Count > 0)
        {
            await OcuparAsientosFuncionAsync(item.IdFuncion.Value, item.Asientos);
        }
    }

    private async Task OcuparAsientosFuncionAsync(int funcionId, List<string> asientos)
    {
        var funcion = await _db.Funcions.FirstOrDefaultAsync(f => f.IdFuncion == funcionId);
        var idSala = funcion?.IdSala ?? 1;

        foreach (var asientoCodigo in asientos)
        {
            await OcuparButacaIndividualAsync(funcionId, idSala, asientoCodigo);
        }
    }

    private async Task OcuparButacaIndividualAsync(int funcionId, int idSala, string asientoCodigo)
    {
        if (string.IsNullOrWhiteSpace(asientoCodigo) || asientoCodigo.Length < 2) return;

        var fila = asientoCodigo.Substring(0, 1);
        if (!int.TryParse(asientoCodigo.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero)) return;

        var butaca = await _db.Butacas
            .FirstOrDefaultAsync(b => b.IdSala == idSala && b.Fila == fila && b.Numero == numero);

        if (butaca == null) return;

        var estado = await _db.EstadoButacaFuncions
            .FirstOrDefaultAsync(e => e.IdFuncion == funcionId && e.IdButaca == butaca.IdButaca);

        if (estado != null)
        {
            estado.Estato = true;
        }
        else
        {
            _db.EstadoButacaFuncions.Add(new EstadoButacaFuncion
            {
                IdFuncion = funcionId,
                IdButaca = butaca.IdButaca,
                Estato = true
            });
        }
    }

    public async Task<PreordenDto?> ObtenerPreordenAsync(string codigoQr)
    {
        if (string.IsNullOrWhiteSpace(codigoQr)) return null;

        var pre = await _db.Preordens
            .Include(p => p.IdClienteNavigation)
            .Include(p => p.IdFuncionNavigation.IdPeliculaNavigation)
            .FirstOrDefaultAsync(p => p.CodigoQr == codigoQr);

        if (pre == null) return null;

        var boletos = await _db.DetallePreordenBoletos
            .Where(b => b.IdPreorden == pre.IdPreorden)
            .ToListAsync();

        var dulceria = await _db.DetallePreordenDulceria
            .Include(d => d.IdProductoNavigation)
            .Where(d => d.IdPreorden == pre.IdPreorden)
            .ToListAsync();

        var boletosCantidad = boletos.Sum(b => b.Caantidad);
        var precioBoleto = pre.IdFuncionNavigation?.Precio ?? 50m;
        var subtotalBoletos = boletosCantidad * precioBoleto;

        var combos = dulceria.Select(d => new PreordenComboDto
        {
            Nombre = d.IdProductoNavigation?.Nombre ?? "Combo",
            Cantidad = d.Cantidad,
            Precio = d.IdProductoNavigation?.PrecioVenta ?? 0m
        }).ToList();

        var subtotalCombos = combos.Sum(c => c.Precio * c.Cantidad);

        return new PreordenDto
        {
            IdPreorden = pre.IdPreorden,
            Codigo = pre.CodigoQr ?? codigoQr,
            Cliente = pre.IdClienteNavigation?.Nombre ?? "Cliente Xilotzin",
            Telefono = pre.IdClienteNavigation?.NumeroTelefono ?? "",
            Pelicula = pre.IdFuncionNavigation?.IdPeliculaNavigation?.Titulo ?? "Película",
            Horario = pre.IdFuncionNavigation?.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "16:00",
            IdFuncion = pre.IdFuncion,
            BoletosGeneral = boletosCantidad,
            PrecioBoleto = precioBoleto,
            Asientos = new List<string> { "C4", "C5" },
            Combos = combos,
            Total = subtotalBoletos + subtotalCombos,
            FechaCreacion = pre.FechaCreacion.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            VigenciaDias = pre.Vigencia,
            EstadoActivo = pre.EstadoActivo
        };
    }

    public async Task<PreordenDto> CrearPreordenAsync(CrearPreordenDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentException("Los datos de la pre-orden son obligatorios.");
        }

        if (dto.IdFuncion <= 0)
        {
            throw new ArgumentException("Debes seleccionar una función válida.");
        }

        if (dto.Boletos < 1 || dto.Boletos > 10)
        {
            throw new ArgumentException("La cantidad de boletos debe ser entre 1 y 10.");
        }

        var nombreLimpio = dto.NombreCliente?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreLimpio) || nombreLimpio.Length < 3)
        {
            throw new ArgumentException("El nombre del cliente debe tener al menos 3 caracteres.");
        }

        var telefonoLimpio = dto.TelefonoCliente?.Trim().Replace(" ", "").Replace("-", "") ?? string.Empty;
        if (telefonoLimpio.Length != 10 || !telefonoLimpio.StartsWith("55") || !telefonoLimpio.All(char.IsDigit))
        {
            throw new ArgumentException("El teléfono debe contener exactamente 10 dígitos e iniciar con 55 (ej. 5512345678).");
        }

        var funcion = await _db.Funcions
            .Include(f => f.IdPeliculaNavigation)
            .Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == dto.IdFuncion);

        if (funcion == null)
        {
            throw new ArgumentException($"No se encontró la función con ID {dto.IdFuncion}.");
        }

        var cliente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.NumeroTelefono == telefonoLimpio);

        if (cliente == null)
        {
            cliente = new Cliente
            {
                Nombre = nombreLimpio,
                NumeroTelefono = telefonoLimpio,
                Correo = $"{telefonoLimpio}@xilotzin.local"
            };
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();
        }
        else if (cliente.Nombre != nombreLimpio)
        {
            cliente.Nombre = nombreLimpio;
            await _db.SaveChangesAsync();
        }

        string folio;
        var random = new Random();
        do
        {
            folio = $"PRE-XIL-{random.Next(10000, 99999)}";
        } while (await _db.Preordens.AnyAsync(p => p.CodigoQr == folio));

        var preorden = new Preorden
        {
            CodigoQr = folio,
            IdFuncion = funcion.IdFuncion,
            IdCliente = cliente.IdCliente,
            FechaCreacion = DateTime.Now,
            Vigencia = 12,
            EstadoActivo = true
        };
        _db.Preordens.Add(preorden);
        await _db.SaveChangesAsync();

        var precioBoleto = funcion.Precio > 0 ? funcion.Precio : 50m;
        var subtotalBoletos = dto.Boletos * precioBoleto;
        var detalleBoleto = new DetallePreordenBoleto
        {
            IdPreorden = preorden.IdPreorden,
            Caantidad = dto.Boletos,
            Subtotal = subtotalBoletos
        };
        _db.DetallePreordenBoletos.Add(detalleBoleto);

        var combosList = new List<PreordenComboDto>();
        decimal subtotalCombos = 0m;

        if (dto.Combos != null && dto.Combos.Count > 0)
        {
            foreach (var comboDto in dto.Combos.Where(c => c.IdProducto > 0 && c.Cantidad > 0))
            {
                var producto = await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == comboDto.IdProducto);
                if (producto != null)
                {
                    var detalleDulce = new DetallePreordenDulcerium
                    {
                        IdPreorden = preorden.IdPreorden,
                        IdProducto = producto.IdProducto,
                        Cantidad = comboDto.Cantidad
                    };
                    _db.DetallePreordenDulceria.Add(detalleDulce);
                    subtotalCombos += producto.PrecioVenta * comboDto.Cantidad;
                    combosList.Add(new PreordenComboDto
                    {
                        Nombre = producto.Nombre,
                        Cantidad = comboDto.Cantidad,
                        Precio = producto.PrecioVenta
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        return new PreordenDto
        {
            IdPreorden = preorden.IdPreorden,
            Codigo = folio,
            Cliente = cliente.Nombre,
            Telefono = cliente.NumeroTelefono ?? telefonoLimpio,
            Pelicula = funcion.IdPeliculaNavigation?.Titulo ?? "Película",
            Horario = funcion.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture),
            IdFuncion = funcion.IdFuncion,
            BoletosGeneral = dto.Boletos,
            PrecioBoleto = precioBoleto,
            Asientos = new List<string>(),
            Combos = combosList,
            Total = subtotalBoletos + subtotalCombos,
            FechaCreacion = preorden.FechaCreacion.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            VigenciaDias = preorden.Vigencia,
            EstadoActivo = preorden.EstadoActivo
        };
    }
}