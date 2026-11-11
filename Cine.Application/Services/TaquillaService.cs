using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cine.Application.Dtos;
using Cine.Application.Exceptions;
using Cine.Application.Interfaces;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Cine.Application.Dtos.Funcion;

namespace Cine.Application.Services;

public class TaquillaService : ITaquillaService
{
    public const string SkuRebanadaPizza = "7501006";
    public const string SkuPizzaCompleta = "7501007";
    public const int FactorRebanadasPorPizza = 8;
    private const string TipoDulceria = "dulceria";
    private const string TipoBoleto = "boleto";
    private const string TipoVideoTrailer = "Trailer";
    private const string PropiedadResults = "results";

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
    private readonly IConfiguration? _config;
    private readonly IOtpCodeStore _otpStore;
    private readonly string _tmdbApiKey;

    public TaquillaService(
        ICineDbContext context,
        HttpClient? httpClient = null,
        IConfiguration? config = null,
        IOtpCodeStore? otpStore = null)
    {
        _db = context;
        _httpClient = httpClient ?? new HttpClient();
        _config = config;
        _otpStore = otpStore ?? new InMemoryOtpCodeStore();
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
            if (doc.RootElement.TryGetProperty(PropiedadResults, out var resultsElement) && resultsElement.ValueKind == JsonValueKind.Array)
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
            var director = crewProp.EnumerateArray()
                .Where(member => GetJsonString(member, "job") == "Director")
                .Select(member => GetJsonString(member, "name"))
                .FirstOrDefault(name => !string.IsNullOrEmpty(name));

            if (!string.IsNullOrEmpty(director))
            {
                return director;
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
            !rdProp.TryGetProperty(PropiedadResults, out var rdResults) ||
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
            !videosProp.TryGetProperty(PropiedadResults, out var vidResults) ||
            vidResults.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? trailerKey = null;
        foreach (var v in vidResults.EnumerateArray())
        {
            var site = GetJsonString(v, "site");
            var type = GetJsonString(v, "type");
            if (site == "YouTube" && (type == TipoVideoTrailer || trailerKey == null))
            {
                var key = GetJsonString(v, "key");
                trailerKey = !string.IsNullOrEmpty(key) ? key : null;
                if (type == TipoVideoTrailer) break;
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

        var pelicula = await _db.Peliculas.FirstOrDefaultAsync(p => p.IdPelicula == funcion.IdPelicula);

        return new FuncionButacasDto
        {
            IdFuncion = funcion.IdFuncion,
            IdSala = funcion.IdSala,
            NombreSala = funcion.IdSalaNavigation?.NombreSala ?? "Sala 1",
            PeliculaTitulo = pelicula?.Titulo ?? string.Empty,
            Horario = funcion.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture),
            Precio = funcion.Precio,
            Butacas = butacasDto
        };
    }

    public async Task<List<ProductoDto>> ObtenerProductosDulceriaAsync()
    {
        var productos = await _db.Productos
            .Include(p => p.IdCategoriaNavigation)
            .Where(p => p.IdCategoriaNavigation == null || p.IdCategoriaNavigation.Nombre != "Taquilla")
            .ToListAsync();

        var stockFraccionado = await _db.InventarioStockFraccionados.FirstOrDefaultAsync();

        return productos
            .Select(p => MapearProductoDto(p, stockFraccionado))
            .ToList();
    }

    private static ProductoDto MapearProductoDto(Producto p, InventarioStockFraccionado? sf)
    {
        var esRebanada = (sf != null && p.IdProducto == sf.IdProductoFraccion)
                         || string.Equals(p.CodigoBarras, SkuRebanadaPizza, StringComparison.OrdinalIgnoreCase);
        var esPizzaCompleta = (sf != null && p.IdProducto == sf.IdProductoMaestro)
                              || string.Equals(p.CodigoBarras, SkuPizzaCompleta, StringComparison.OrdinalIgnoreCase);
        var esFraccionado = esRebanada || esPizzaCompleta;
        var factor = sf?.FactorFraccion > 0 ? sf.FactorFraccion : FactorRebanadasPorPizza;

        var stockCalculado = p.StockActual;
        if (sf != null)
        {
            if (esRebanada)
            {
                stockCalculado = sf.StockFraccionesActual;
            }
            else if (esPizzaCompleta)
            {
                stockCalculado = sf.StockFraccionesActual / factor;
            }
        }

        string? descripcionDescuento = null;
        if (esRebanada)
        {
            descripcionDescuento = "Descuenta 1 rebanada del stock";
        }
        else if (esPizzaCompleta)
        {
            descripcionDescuento = $"Descuenta {factor} rebanadas del stock";
        }

        return new ProductoDto
        {
            IdProducto = p.IdProducto,
            Sku = p.CodigoBarras,
            Nombre = p.Nombre,
            Categoria = p.IdCategoriaNavigation?.Nombre ?? "Snacks",
            Precio = p.PrecioVenta,
            Stock = stockCalculado,
            Img = ImagenesDulceria.TryGetValue(p.CodigoBarras, out var imgUrl)
                ? imgUrl
                : "https://images.unsplash.com/photo-1585647347384-2593bc35786b?q=80&w=400",
            EsFraccionado = esFraccionado,
            FactorFraccion = esPizzaCompleta ? factor : 1,
            DescripcionDescuento = descripcionDescuento
        };
    }

    public async Task<CatalogoDulceriaPosDto> ObtenerCatalogoDulceriaPosAsync()
    {
        var todos = await ObtenerProductosDulceriaAsync();
        var stockFraccionadoEntidad = await _db.InventarioStockFraccionados.FirstOrDefaultAsync();
        var monitor = ConstruirMonitorStockFraccionado(todos, stockFraccionadoEntidad, 0);

        return new CatalogoDulceriaPosDto
        {
            StockFraccionado = monitor,
            SnacksYBebidas = todos.Where(p => !p.EsFraccionado).ToList(),
            TodosLosProductos = todos
        };
    }

    private static StockFraccionadoDto? ConstruirMonitorStockFraccionado(
        List<ProductoDto> productos,
        InventarioStockFraccionado? sf,
        int rebanadasDescontadasEnOrden)
    {
        var prodRebanada = productos.FirstOrDefault(p =>
            (sf != null && p.IdProducto == sf.IdProductoFraccion) ||
            string.Equals(p.Sku, SkuRebanadaPizza, StringComparison.OrdinalIgnoreCase));

        var prodPizza = productos.FirstOrDefault(p =>
            (sf != null && p.IdProducto == sf.IdProductoMaestro) ||
            string.Equals(p.Sku, SkuPizzaCompleta, StringComparison.OrdinalIgnoreCase));

        if (sf == null && prodRebanada == null && prodPizza == null)
        {
            return null;
        }

        var factor = sf?.FactorFraccion > 0 ? sf.FactorFraccion : FactorRebanadasPorPizza;
        var rebanadasBase = sf?.StockFraccionesActual
                            ?? prodRebanada?.Stock
                            ?? ((prodPizza?.Stock ?? 0) * factor);

        var rebanadasRestantes = Math.Max(0, rebanadasBase - rebanadasDescontadasEnOrden);
        var pizzasEquivalentes = rebanadasRestantes / factor;
        var rebanadasSueltas = rebanadasRestantes % factor;

        var clonRebanada = prodRebanada != null ? ClonarProductoConStock(prodRebanada, rebanadasRestantes) : null;
        var clonPizza = prodPizza != null ? ClonarProductoConStock(prodPizza, pizzasEquivalentes) : null;

        return new StockFraccionadoDto
        {
            IdStockFraccionado = sf?.IdStockFraccionado ?? 1,
            NombreInsumo = "PIZZAS",
            StockRebanadasActual = rebanadasRestantes,
            FactorFraccion = factor,
            PizzasCompletasEquivalentes = pizzasEquivalentes,
            RebanadasSueltas = rebanadasSueltas,
            EtiquetaMonitor = $"Stock: {rebanadasRestantes} rebanadas ({pizzasEquivalentes} pizzas)",
            ProductoRebanada = clonRebanada,
            ProductoPizzaCompleta = clonPizza
        };
    }

    private static ProductoDto ClonarProductoConStock(ProductoDto origen, int nuevoStock)
    {
        return new ProductoDto
        {
            IdProducto = origen.IdProducto,
            Sku = origen.Sku,
            Nombre = origen.Nombre,
            Categoria = origen.Categoria,
            Precio = origen.Precio,
            Stock = nuevoStock,
            Img = origen.Img,
            EsFraccionado = origen.EsFraccionado,
            FactorFraccion = origen.FactorFraccion,
            DescripcionDescuento = origen.DescripcionDescuento
        };
    }

    public async Task<ProductoDto?> BuscarProductoPorSkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("El código de barras (SKU) es obligatorio.");
        }

        var skuLimpio = sku.Trim();
        var productos = await ObtenerProductosDulceriaAsync();
        var encontrado = productos.FirstOrDefault(p =>
            string.Equals(p.Sku, skuLimpio, StringComparison.OrdinalIgnoreCase));

        if (encontrado == null)
        {
            return null;
        }

        if (encontrado.Stock <= 0)
        {
            throw new InvalidOperationException(
                $"El artículo '{encontrado.Nombre}' (SKU: {encontrado.Sku}) tiene inventario 0 y no puede agregarse a la venta.");
        }

        return encontrado;
    }

    public async Task<ResumenOrdenCalculadoDto> CalcularOrdenPosAsync(CalcularOrdenRequestDto request)
    {
        if (request == null)
        {
            throw new ArgumentException("La solicitud de cálculo de orden es obligatoria.");
        }

        var productosCatalogo = await ObtenerProductosDulceriaAsync();
        var sfEntidad = await _db.InventarioStockFraccionados.FirstOrDefaultAsync();

        var lineas = ClonarLineasEntrada(request.ItemsActuales);
        ProductoDto? productoAgregado = null;

        if (!string.IsNullOrWhiteSpace(request.SkuAgregar) || request.IdProductoAgregar.HasValue)
        {
            productoAgregado = ResolverProductoSolicitado(request, productosCatalogo);
            AplicarCambioEnLineas(lineas, productoAgregado, request.CantidadAgregar);
        }

        var rebanadasConsumidas = ValidarYCalcularRebanadasEnOrden(lineas, productosCatalogo, sfEntidad);
        var monitorActualizado = ConstruirMonitorStockFraccionado(productosCatalogo, sfEntidad, rebanadasConsumidas);
        var productosActualizados = CalcularCatalogoConStockRestante(productosCatalogo, lineas, monitorActualizado);

        return ConstruirResumenCalculado(lineas, productosCatalogo, monitorActualizado, productosActualizados, productoAgregado);
    }

    private static List<RegistrarVentaItemDto> ClonarLineasEntrada(List<RegistrarVentaItemDto>? items)
    {
        if (items == null) return new List<RegistrarVentaItemDto>();

        return items
            .Where(i => i.Cantidad > 0)
            .Select(i => new RegistrarVentaItemDto
            {
                IdProducto = i.IdProducto,
                IdFuncion = i.IdFuncion,
                Sku = i.Sku,
                Nombre = i.Nombre,
                Tipo = string.IsNullOrWhiteSpace(i.Tipo) ? TipoDulceria : i.Tipo,
                Cantidad = i.Cantidad,
                Precio = i.Precio,
                Asientos = i.Asientos != null ? new List<string>(i.Asientos) : null,
                NombreSala = i.NombreSala,
                EsPreorden = i.EsPreorden,
                CodigoPreorden = i.CodigoPreorden
            })
            .ToList();
    }

    private static ProductoDto ResolverProductoSolicitado(CalcularOrdenRequestDto request, List<ProductoDto> catalogo)
    {
        ProductoDto? producto = null;

        if (request.IdProductoAgregar.HasValue && request.IdProductoAgregar.Value > 0)
        {
            producto = catalogo.FirstOrDefault(p => p.IdProducto == request.IdProductoAgregar.Value);
        }

        if (producto == null && !string.IsNullOrWhiteSpace(request.SkuAgregar))
        {
            var skuLimpio = request.SkuAgregar.Trim();
            producto = catalogo.FirstOrDefault(p =>
                string.Equals(p.Sku, skuLimpio, StringComparison.OrdinalIgnoreCase));
        }

        if (producto == null)
        {
            throw new KeyNotFoundException("No se encontró el producto con el código de barras (SKU) especificado.");
        }

        return producto;
    }

    private static void AplicarCambioEnLineas(List<RegistrarVentaItemDto> lineas, ProductoDto producto, int deltaCantidad)
    {
        if (deltaCantidad == 0)
        {
            throw new ArgumentException("La cantidad a agregar o modificar debe ser distinta de cero.");
        }

        var existente = lineas.FirstOrDefault(l =>
            !l.EsPreorden &&
            l.Tipo == TipoDulceria &&
            ((l.IdProducto.HasValue && l.IdProducto.Value == producto.IdProducto) ||
             (!string.IsNullOrWhiteSpace(l.Sku) && string.Equals(l.Sku, producto.Sku, StringComparison.OrdinalIgnoreCase))));

        if (existente == null)
        {
            if (deltaCantidad < 0) return;

            lineas.Add(new RegistrarVentaItemDto
            {
                IdProducto = producto.IdProducto,
                Sku = producto.Sku,
                Nombre = producto.Nombre,
                Tipo = TipoDulceria,
                Cantidad = deltaCantidad,
                Precio = producto.Precio
            });
            return;
        }

        existente.Cantidad += deltaCantidad;
        existente.Precio = producto.Precio;
        existente.Nombre = producto.Nombre;
        existente.Sku = producto.Sku;
        existente.IdProducto = producto.IdProducto;

        if (existente.Cantidad <= 0)
        {
            lineas.Remove(existente);
        }
    }

    private static int ValidarYCalcularRebanadasEnOrden(
        List<RegistrarVentaItemDto> lineas,
        List<ProductoDto> catalogo,
        InventarioStockFraccionado? sf)
    {
        var factor = sf?.FactorFraccion > 0 ? sf.FactorFraccion : FactorRebanadasPorPizza;
        var prodRebanada = catalogo.FirstOrDefault(p =>
            (sf != null && p.IdProducto == sf.IdProductoFraccion) ||
            string.Equals(p.Sku, SkuRebanadaPizza, StringComparison.OrdinalIgnoreCase));
        var prodPizza = catalogo.FirstOrDefault(p =>
            (sf != null && p.IdProducto == sf.IdProductoMaestro) ||
            string.Equals(p.Sku, SkuPizzaCompleta, StringComparison.OrdinalIgnoreCase));

        var rebanadasDisponiblesBase = sf?.StockFraccionesActual
                                       ?? prodRebanada?.Stock
                                       ?? ((prodPizza?.Stock ?? 0) * factor);

        int rebanadasSolicitadas = 0;

        foreach (var linea in lineas.Where(l => l.Tipo == TipoDulceria))
        {
            var prod = BuscarProductoEnCatalogo(linea, catalogo);
            if (prod == null) continue;

            if (prod.Stock <= 0)
            {
                throw new InvalidOperationException(
                    $"El artículo '{prod.Nombre}' tiene inventario actual 0 y no puede agregarse a la venta.");
            }

            if (prod.EsFraccionado)
            {
                rebanadasSolicitadas += linea.Cantidad * prod.FactorFraccion;
            }
            else if (linea.Cantidad > prod.Stock)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{prod.Nombre}'. Disponible en inventario: {prod.Stock}.");
            }
        }

        if (rebanadasSolicitadas > 0 && rebanadasSolicitadas > rebanadasDisponiblesBase)
        {
            throw new InvalidOperationException(
                $"Stock fraccionado insuficiente de Pizzas. Requerido: {rebanadasSolicitadas} rebanadas, Disponible: {rebanadasDisponiblesBase} rebanadas.");
        }

        return rebanadasSolicitadas;
    }

    private static ProductoDto? BuscarProductoEnCatalogo(RegistrarVentaItemDto linea, List<ProductoDto> catalogo)
    {
        if (linea.IdProducto.HasValue && linea.IdProducto.Value > 0)
        {
            var porId = catalogo.FirstOrDefault(p => p.IdProducto == linea.IdProducto.Value);
            if (porId != null) return porId;
        }

        if (!string.IsNullOrWhiteSpace(linea.Sku))
        {
            var porSku = catalogo.FirstOrDefault(p => string.Equals(p.Sku, linea.Sku, StringComparison.OrdinalIgnoreCase));
            if (porSku != null) return porSku;
        }

        return catalogo.FirstOrDefault(p => string.Equals(p.Nombre, linea.Nombre, StringComparison.OrdinalIgnoreCase));
    }

    private static List<ProductoDto> CalcularCatalogoConStockRestante(
        List<ProductoDto> catalogoBase,
        List<RegistrarVentaItemDto> lineas,
        StockFraccionadoDto? monitor)
    {
        var resultado = new List<ProductoDto>(catalogoBase.Count);

        foreach (var prod in catalogoBase)
        {
            if (prod.EsFraccionado && monitor != null)
            {
                var nuevoStock = prod.FactorFraccion > 1
                    ? monitor.PizzasCompletasEquivalentes
                    : monitor.StockRebanadasActual;
                resultado.Add(ClonarProductoConStock(prod, nuevoStock));
                continue;
            }

            var cantidadEnCarrito = lineas
                .Where(l => l.Tipo == TipoDulceria &&
                            ((l.IdProducto.HasValue && l.IdProducto.Value == prod.IdProducto) ||
                             string.Equals(l.Sku, prod.Sku, StringComparison.OrdinalIgnoreCase)))
                .Sum(l => l.Cantidad);

            resultado.Add(ClonarProductoConStock(prod, Math.Max(0, prod.Stock - cantidadEnCarrito)));
        }

        return resultado;
    }

    private static ResumenOrdenCalculadoDto ConstruirResumenCalculado(
        List<RegistrarVentaItemDto> lineas,
        List<ProductoDto> catalogo,
        StockFraccionadoDto? monitor,
        List<ProductoDto> productosActualizados,
        ProductoDto? productoAgregado)
    {
        var itemsCalculados = new List<OrdenLineaCalculadaDto>();
        decimal subtotalBoletos = 0m;
        decimal subtotalDulceria = 0m;
        int idx = 1;

        foreach (var linea in lineas)
        {
            var esBoleto = string.Equals(linea.Tipo, TipoBoleto, StringComparison.OrdinalIgnoreCase);
            var prodCatalogo = esBoleto ? null : BuscarProductoEnCatalogo(linea, catalogo);
            var precioUnitario = prodCatalogo?.Precio ?? linea.Precio;
            var subtotalLinea = precioUnitario * linea.Cantidad;

            if (esBoleto)
            {
                subtotalBoletos += subtotalLinea;
            }
            else
            {
                subtotalDulceria += subtotalLinea;
            }

            var idLinea = esBoleto
                ? $"bol_{linea.IdFuncion ?? 0}_{idx}"
                : $"snk_{prodCatalogo?.IdProducto ?? linea.IdProducto ?? idx}";

            itemsCalculados.Add(new OrdenLineaCalculadaDto
            {
                IdLinea = idLinea,
                IdProducto = prodCatalogo?.IdProducto ?? linea.IdProducto,
                IdFuncion = linea.IdFuncion,
                Sku = prodCatalogo?.Sku ?? linea.Sku,
                Nombre = prodCatalogo?.Nombre ?? linea.Nombre,
                Tipo = esBoleto ? TipoBoleto : TipoDulceria,
                EtiquetaCategoria = esBoleto ? "TAQUILLA" : "DULCERÍA",
                Cantidad = linea.Cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = subtotalLinea,
                Asientos = linea.Asientos,
                NombreSala = linea.NombreSala,
                EsPreorden = linea.EsPreorden,
                CodigoPreorden = linea.CodigoPreorden
            });
            idx++;
        }

        return new ResumenOrdenCalculadoDto
        {
            Items = itemsCalculados,
            TotalArticulos = itemsCalculados.Sum(i => i.Cantidad),
            SubtotalBoletos = subtotalBoletos,
            SubtotalDulceria = subtotalDulceria,
            TotalVenta = subtotalBoletos + subtotalDulceria,
            StockFraccionado = monitor,
            ProductosActualizados = productosActualizados,
            ProductoAgregado = productoAgregado
        };
    }

    public async Task<VentaConfirmadaDto> RegistrarVentaAsync(RegistrarVentaDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("No se pueden registrar ventas sin artículos.");
        }

        // Validación de colisión de asientos (HU-06)
        await ValidarDisponibilidadAsientosAsync(dto.Items);

        // Validación de inventario y stock fraccionado de dulcería (HU-07)
        await ValidarDisponibilidadDulceriaAsync(dto.Items);

        var totalCalculado = dto.Items.Sum(i => i.Precio * i.Cantidad);
        var metodoNormalizado = string.IsNullOrWhiteSpace(dto.MetodoPago) ? "Efectivo" : dto.MetodoPago.Trim();
        var (montoRecibido, cambioCalculado) = ValidarYCalcularPago(dto, totalCalculado, metodoNormalizado);
        var codigoPreorden = ExtraerCodigoPreorden(dto);

        Preorden? preordenParaCanjear = null;
        if (!string.IsNullOrWhiteSpace(codigoPreorden))
        {
            preordenParaCanjear = await ValidarPreordenParaVentaAsync(codigoPreorden);
        }

        var idVendedorFinal = dto.IdVendedor > 0 ? dto.IdVendedor : 1;
        var venta = new Ventum
        {
            IdVendedor = idVendedorFinal,
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

        var codigoBarrasTicket = GenerarCodigoBarrasTicket(venta.IdVenta, venta.FechaVenta);
        RegistrarPagoEnBd(venta, metodoNormalizado, montoRecibido, cambioCalculado, dto.FolioAutorizacion, codigoBarrasTicket);

        if (preordenParaCanjear != null)
        {
            RegistrarCanjePreordenEnBd(preordenParaCanjear, venta.IdVenta, idVendedorFinal, totalCalculado);
        }

        await _db.SaveChangesAsync();

        return new VentaConfirmadaDto
        {
            IdVenta = venta.IdVenta,
            Folio = $"VT-{venta.IdVenta:D5}",
            Total = venta.Total,
            MetodoPago = metodoNormalizado,
            MontoRecibido = montoRecibido,
            Cambio = cambioCalculado,
            FolioAutorizacion = dto.FolioAutorizacion,
            RfcCine = "CTX-260414-9X0",
            NombreCine = "CINE TEATRO XILOTZIN",
            DireccionCine = "JILOTEPEC, ESTADO DE MÉXICO",
            TelefonoCine = "(55) 1234 5678",
            CodigoBarrasTicket = codigoBarrasTicket,
            CodigoPreordenCanjeada = preordenParaCanjear?.CodigoQr ?? codigoPreorden,
            FechaVenta = venta.FechaVenta,
            Items = dto.Items
        };
    }

    private static (decimal MontoRecibido, decimal Cambio) ValidarYCalcularPago(
        RegistrarVentaDto dto,
        decimal totalCalculado,
        string metodoNormalizado)
    {
        var esEfectivo = string.Equals(metodoNormalizado, "Efectivo", StringComparison.OrdinalIgnoreCase);

        if (!esEfectivo && dto.FolioAutorizacion != null && string.IsNullOrWhiteSpace(dto.FolioAutorizacion))
        {
            throw new ArgumentException("El folio de autorización (voucher) de la terminal no puede estar vacío.");
        }

        var montoRecibido = dto.MontoRecibido ?? totalCalculado;

        if (esEfectivo && dto.MontoRecibido.HasValue && dto.MontoRecibido.Value < totalCalculado)
        {
            throw new ArgumentException(
                $"El efectivo recibido (${dto.MontoRecibido.Value:F2}) es menor al monto total a cobrar (${totalCalculado:F2}).");
        }

        var cambio = esEfectivo ? Math.Max(0m, montoRecibido - totalCalculado) : 0m;
        return (montoRecibido, cambio);
    }

    private static string? ExtraerCodigoPreorden(RegistrarVentaDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.CodigoPreorden))
        {
            return dto.CodigoPreorden.Trim().ToUpperInvariant();
        }

        var desdeItem = dto.Items.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.CodigoPreorden))?.CodigoPreorden;
        return string.IsNullOrWhiteSpace(desdeItem) ? null : desdeItem.Trim().ToUpperInvariant();
    }

    private async Task<Preorden?> ValidarPreordenParaVentaAsync(string codigoPreorden)
    {
        var preorden = await _db.Preordens.FirstOrDefaultAsync(p => p.CodigoQr == codigoPreorden);
        if (preorden == null) return null;

        var yaCanjeada = !preorden.EstadoActivo ||
                         await _db.CanjePreordens.AnyAsync(c => c.IdPreorden == preorden.IdPreorden);
        if (yaCanjeada)
        {
            throw new InvalidOperationException($"El código QR '{codigoPreorden}' ya se encuentra marcado como Canjeado.");
        }

        var dias = Math.Max(0, (int)(DateTime.Today - preorden.FechaCreacion.Date).TotalDays);
        if (dias > 12)
        {
            throw new InvalidOperationException($"El código QR '{codigoPreorden}' supera los 12 días de vigencia y ha expirado.");
        }

        return preorden;
    }

    private static string GenerarCodigoBarrasTicket(int idVenta, DateTime fechaVenta)
    {
        return $"75010{fechaVenta:MMdd}{idVenta:D4}";
    }

    private void RegistrarPagoEnBd(
        Ventum venta,
        string metodoNormalizado,
        decimal montoRecibido,
        decimal cambioCalculado,
        string? folioAutorizacion,
        string codigoBarrasTicket)
    {
        _db.PagoVentas.Add(new PagoVenta
        {
            IdVenta = venta.IdVenta,
            MetodoPago = metodoNormalizado,
            MontoTotal = venta.Total,
            MontoRecibido = montoRecibido,
            Cambio = cambioCalculado,
            FolioAutorizacion = string.IsNullOrWhiteSpace(folioAutorizacion) ? null : folioAutorizacion.Trim(),
            CodigoBarrasTicket = codigoBarrasTicket,
            RfcEmisor = "CTX-260414-9X0",
            FechaPago = venta.FechaVenta
        });
    }

    private void RegistrarCanjePreordenEnBd(Preorden preorden, int idVenta, int idVendedor, decimal montoCanjeado)
    {
        preorden.EstadoActivo = false;
        _db.CanjePreordens.Add(new CanjePreorden
        {
            IdPreorden = preorden.IdPreorden,
            IdVenta = idVenta,
            IdVendedor = idVendedor,
            CodigoQr = preorden.CodigoQr ?? string.Empty,
            FechaCanje = DateTime.Now,
            MontoCanjeado = montoCanjeado
        });
    }

    private async Task ValidarDisponibilidadDulceriaAsync(List<RegistrarVentaItemDto> items)
    {
        var itemsDulceria = items
            .Where(i => i.Tipo == TipoDulceria || i.IdProducto.HasValue)
            .ToList();

        if (itemsDulceria.Count == 0) return;

        var sf = await _db.InventarioStockFraccionados.FirstOrDefaultAsync();
        var factor = sf?.FactorFraccion > 0 ? sf.FactorFraccion : FactorRebanadasPorPizza;
        int rebanadasTotalesRequeridas = 0;

        foreach (var item in itemsDulceria)
        {
            rebanadasTotalesRequeridas += await ValidarYContarRebanadasDeItemAsync(item, sf, factor);
        }

        if (rebanadasTotalesRequeridas > 0)
        {
            await ValidarRebanadasDisponiblesAsync(sf, factor, rebanadasTotalesRequeridas);
        }
    }

    private async Task<int> ValidarYContarRebanadasDeItemAsync(
        RegistrarVentaItemDto item,
        InventarioStockFraccionado? sf,
        int factor)
    {
        if (item.Cantidad <= 0)
        {
            throw new ArgumentException("La cantidad de cada artículo de dulcería debe ser mayor a cero.");
        }

        var producto = await BuscarEntidadProductoAsync(item);
        if (producto == null) return 0;

        if (producto.StockActual <= 0)
        {
            throw new InvalidOperationException(
                $"El artículo '{producto.Nombre}' no tiene inventario disponible (stock actual: 0).");
        }

        if (EsProductoRebanada(producto, sf))
        {
            return item.Cantidad;
        }

        if (EsProductoPizzaCompleta(producto, sf))
        {
            return item.Cantidad * factor;
        }

        if (item.Cantidad > producto.StockActual)
        {
            throw new InvalidOperationException(
                $"Stock insuficiente para '{producto.Nombre}'. Solicitado: {item.Cantidad}, Disponible: {producto.StockActual}.");
        }

        return 0;
    }

    private static bool EsProductoRebanada(Producto producto, InventarioStockFraccionado? sf) =>
        (sf != null && producto.IdProducto == sf.IdProductoFraccion) ||
        string.Equals(producto.CodigoBarras, SkuRebanadaPizza, StringComparison.OrdinalIgnoreCase);

    private static bool EsProductoPizzaCompleta(Producto producto, InventarioStockFraccionado? sf) =>
        (sf != null && producto.IdProducto == sf.IdProductoMaestro) ||
        string.Equals(producto.CodigoBarras, SkuPizzaCompleta, StringComparison.OrdinalIgnoreCase);

    private async Task ValidarRebanadasDisponiblesAsync(InventarioStockFraccionado? sf, int factor, int rebanadasRequeridas)
    {
        int disponible;
        if (sf != null)
        {
            disponible = sf.StockFraccionesActual;
        }
        else
        {
            var prodReb = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == SkuRebanadaPizza);
            var prodPiz = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == SkuPizzaCompleta);
            disponible = prodReb?.StockActual ?? ((prodPiz?.StockActual ?? 0) * factor);
        }

        if (rebanadasRequeridas > disponible)
        {
            throw new InvalidOperationException(
                $"Stock fraccionado insuficiente de Pizzas. Requerido: {rebanadasRequeridas} rebanadas, Disponible: {disponible} rebanadas.");
        }
    }

    private async Task<Producto?> BuscarEntidadProductoAsync(RegistrarVentaItemDto item)
    {
        if (item.IdProducto.HasValue && item.IdProducto.Value > 0)
        {
            return await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto.Value);
        }

        return await _db.Productos.FirstOrDefaultAsync(p =>
            p.CodigoBarras == item.Sku || p.Nombre == item.Nombre);
    }

    private async Task ValidarDisponibilidadAsientosAsync(List<RegistrarVentaItemDto> items)
    {
        var boletosConAsientos = items
            .Where(i => i.Tipo == TipoBoleto && i.IdFuncion.HasValue && i.Asientos != null && i.Asientos.Count > 0)
            .ToList();

        if (boletosConAsientos.Count == 0) return;

        foreach (var grupo in boletosConAsientos.GroupBy(i => i.IdFuncion!.Value))
        {
            await ValidarGrupoAsientosFuncionAsync(grupo.Key, grupo.SelectMany(i => i.Asientos!).ToList());
        }
    }

    private async Task ValidarGrupoAsientosFuncionAsync(int funcionId, List<string> todosLosAsientos)
    {
        if (todosLosAsientos.Distinct(StringComparer.OrdinalIgnoreCase).Count() != todosLosAsientos.Count)
        {
            throw new ArgumentException("No se pueden seleccionar asientos duplicados en la misma orden.");
        }

        var funcion = await _db.Funcions.FirstOrDefaultAsync(f => f.IdFuncion == funcionId);
        if (funcion == null)
        {
            throw new ArgumentException($"No se encontró la función con ID {funcionId}.");
        }

        foreach (var asientoCodigo in todosLosAsientos)
        {
            await VerificarButacaOcupadaAsync(funcionId, funcion.IdSala, asientoCodigo);
        }
    }

    private async Task VerificarButacaOcupadaAsync(int funcionId, int idSala, string asientoCodigo)
    {
        if (string.IsNullOrWhiteSpace(asientoCodigo) || asientoCodigo.Length < 2) return;

        var fila = asientoCodigo.Substring(0, 1);
        if (!int.TryParse(asientoCodigo.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero)) return;

        var butaca = await _db.Butacas
            .FirstOrDefaultAsync(b => b.IdSala == idSala && b.Fila == fila && b.Numero == numero);

        if (butaca == null) return;

        var estaOcupada = await _db.EstadoButacaFuncions
            .AnyAsync(e => e.IdFuncion == funcionId && e.IdButaca == butaca.IdButaca && e.Estato);

        if (estaOcupada)
        {
            throw new InvalidOperationException($"La butaca {asientoCodigo} ya se encuentra ocupada para esta función.");
        }
    }

    private async Task ProcesarItemVentaAsync(int idVenta, RegistrarVentaItemDto item, Producto? productoBoleto)
    {
        if (item.Tipo == TipoDulceria || item.IdProducto.HasValue)
        {
            await ProcesarItemDulceriaAsync(idVenta, item);
        }
        else if (item.Tipo == TipoBoleto)
        {
            await ProcesarItemBoletoAsync(idVenta, item, productoBoleto);
        }
    }

    private async Task ProcesarItemDulceriaAsync(int idVenta, RegistrarVentaItemDto item)
    {
        var producto = await BuscarEntidadProductoAsync(item);
        if (producto == null) return;

        _db.DetalleVenta.Add(new DetalleVentum
        {
            IdVenta = idVenta,
            IdProducto = producto.IdProducto,
            Cantidad = item.Cantidad
        });

        var sf = await _db.InventarioStockFraccionados.FirstOrDefaultAsync(s =>
            s.IdProductoFraccion == producto.IdProducto || s.IdProductoMaestro == producto.IdProducto);

        var esRebanada = (sf != null && producto.IdProducto == sf.IdProductoFraccion)
                         || string.Equals(producto.CodigoBarras, SkuRebanadaPizza, StringComparison.OrdinalIgnoreCase);
        var esPizzaCompleta = (sf != null && producto.IdProducto == sf.IdProductoMaestro)
                              || string.Equals(producto.CodigoBarras, SkuPizzaCompleta, StringComparison.OrdinalIgnoreCase);

        if (esRebanada || esPizzaCompleta)
        {
            await DescontarStockFraccionadoAsync(producto, item.Cantidad, esPizzaCompleta, sf);
            return;
        }

        producto.StockActual = Math.Max(0, producto.StockActual - item.Cantidad);
    }

    private async Task DescontarStockFraccionadoAsync(
        Producto productoVendido,
        int cantidadVendida,
        bool esPizzaCompleta,
        InventarioStockFraccionado? sf)
    {
        var factor = sf?.FactorFraccion > 0 ? sf.FactorFraccion : FactorRebanadasPorPizza;
        var rebanadasADescontar = esPizzaCompleta ? cantidadVendida * factor : cantidadVendida;

        var prodRebanada = sf != null
            ? await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == sf.IdProductoFraccion)
            : await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == SkuRebanadaPizza);

        var prodPizza = sf != null
            ? await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == sf.IdProductoMaestro)
            : await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == SkuPizzaCompleta);

        int stockRebanadasActual = sf?.StockFraccionesActual
                                   ?? prodRebanada?.StockActual
                                   ?? ((prodPizza?.StockActual ?? 0) * factor);

        int nuevoStockRebanadas = Math.Max(0, stockRebanadasActual - rebanadasADescontar);
        int nuevoStockPizzas = nuevoStockRebanadas / factor;

        if (sf != null)
        {
            sf.StockFraccionesActual = nuevoStockRebanadas;
            sf.UltimaActualizacion = DateTime.Now;
        }

        if (prodRebanada != null)
        {
            prodRebanada.StockActual = nuevoStockRebanadas;
        }

        if (prodPizza != null)
        {
            prodPizza.StockActual = nuevoStockPizzas;
        }

        if (prodRebanada == null && prodPizza == null)
        {
            productoVendido.StockActual = esPizzaCompleta ? nuevoStockPizzas : nuevoStockRebanadas;
        }
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

        var codigoLimpio = codigoQr.Trim().ToUpperInvariant();
        var pre = await _db.Preordens
            .Include(p => p.IdClienteNavigation)
            .Include(p => p.IdFuncionNavigation.IdPeliculaNavigation)
            .FirstOrDefaultAsync(p => p.CodigoQr != null && p.CodigoQr.ToUpper() == codigoLimpio);

        if (pre == null) return null;

        return await ConstruirPreordenDtoAsync(pre, codigoLimpio);
    }

    public async Task<PreordenDto> ValidarPreordenQrAsync(string codigoQr)
    {
        if (string.IsNullOrWhiteSpace(codigoQr))
        {
            throw new ArgumentException("El código o folio QR es obligatorio.");
        }

        var preorden = await ObtenerPreordenAsync(codigoQr);
        if (preorden == null)
        {
            throw new KeyNotFoundException($"No se encontró ninguna pre-orden con el folio '{codigoQr.Trim().ToUpperInvariant()}'.");
        }

        if (preorden.Canjeada)
        {
            throw new InvalidOperationException(
                preorden.MotivoRechazo ?? "El código QR ya se encuentra marcado como Canjeado en la base de datos.");
        }

        if (preorden.Expirada)
        {
            throw new InvalidOperationException(
                preorden.MotivoRechazo ?? "El código QR supera los 12 días de vigencia y ha expirado.");
        }

        if (preorden.BoletosGeneral <= 0)
        {
            throw new InvalidOperationException("La pre-orden no contiene boletos para canjear.");
        }

        return preorden;
    }

    public async Task<List<PreordenDto>> ObtenerPreordenesActivasAsync()
    {
        var preordenes = await _db.Preordens
            .Include(p => p.IdClienteNavigation)
            .Include(p => p.IdFuncionNavigation.IdPeliculaNavigation)
            .Where(p => p.EstadoActivo)
            .OrderByDescending(p => p.FechaCreacion)
            .Take(20)
            .ToListAsync();

        var resultado = new List<PreordenDto>();
        foreach (var pre in preordenes)
        {
            var dto = await ConstruirPreordenDtoAsync(pre, pre.CodigoQr ?? string.Empty);
            if (dto.EstadoActivo && !dto.Canjeada && !dto.Expirada)
            {
                resultado.Add(dto);
            }
        }

        return resultado;
    }

    private async Task<PreordenDto> ConstruirPreordenDtoAsync(Preorden pre, string codigoFallback)
    {
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
            IdProducto = d.IdProducto,
            Sku = d.IdProductoNavigation?.CodigoBarras ?? string.Empty,
            Nombre = d.IdProductoNavigation?.Nombre ?? "Combo",
            Cantidad = d.Cantidad,
            Precio = d.IdProductoNavigation?.PrecioVenta ?? 0m
        }).ToList();

        var subtotalCombos = combos.Sum(c => c.Precio * c.Cantidad);
        var diasTranscurridos = Math.Max(0, (int)(DateTime.Today - pre.FechaCreacion.Date).TotalDays);
        var yaCanjeada = !pre.EstadoActivo ||
                         await _db.CanjePreordens.AnyAsync(c => c.IdPreorden == pre.IdPreorden);
        var expirada = diasTranscurridos > 12;

        return new PreordenDto
        {
            IdPreorden = pre.IdPreorden,
            Codigo = pre.CodigoQr ?? codigoFallback,
            Cliente = pre.IdClienteNavigation?.Nombre ?? "Cliente Xilotzin",
            Telefono = pre.IdClienteNavigation?.NumeroTelefono ?? "",
            Pelicula = pre.IdFuncionNavigation?.IdPeliculaNavigation?.Titulo ?? "Película",
            Horario = pre.IdFuncionNavigation?.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "16:00",
            NombreSala = pre.IdFuncionNavigation?.IdSalaNavigation?.NombreSala ?? "Sala 1",
            IdFuncion = pre.IdFuncion,
            BoletosGeneral = boletosCantidad,
            PrecioBoleto = precioBoleto,
            Asientos = new List<string>(),
            Combos = combos,
            Total = subtotalBoletos + subtotalCombos,
            FechaCreacion = pre.FechaCreacion.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            VigenciaDias = pre.Vigencia,
            DiasTranscurridos = diasTranscurridos,
            Expirada = expirada,
            Canjeada = yaCanjeada,
            MotivoRechazo = ObtenerMotivoRechazoPreorden(yaCanjeada, expirada, diasTranscurridos),
            EstadoActivo = pre.EstadoActivo && !yaCanjeada && !expirada
        };

        return MapPreordenToDto(pre, boletos, dulceria, codigoQr);

    }

    private static string? ObtenerMotivoRechazoPreorden(bool yaCanjeada, bool expirada, int diasTranscurridos)
    {
        if (yaCanjeada)
        {
            return "El código QR ya fue canjeado anteriormente o se encuentra inactivo.";
        }
        if (expirada)
        {
            return $"El código QR supera los 12 días de vigencia ({diasTranscurridos} días transcurridos).";
        }
        return null;
    }

    public async Task<PreordenDto> CrearPreordenAsync(CrearPreordenDto dto)
    {
        var (nombreLimpio, telefonoLimpio) = ValidarSolicitudCrearPreorden(dto);

        var funcion = await _db.Funcions
            .Include(f => f.IdPeliculaNavigation)
            .Include(f => f.IdSalaNavigation)
            .FirstOrDefaultAsync(f => f.IdFuncion == dto.IdFuncion);

        if (funcion == null)
        {
            throw new ArgumentException($"No se encontró la función con ID {dto.IdFuncion}.");
        }

        var cliente = await ObtenerOActualizarClientePreordenAsync(nombreLimpio, telefonoLimpio);
        var folio = await GenerarFolioPreordenUnicoAsync();

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
        _db.DetallePreordenBoletos.Add(new DetallePreordenBoleto
        {
            IdPreorden = preorden.IdPreorden,
            Caantidad = dto.Boletos,
            Subtotal = subtotalBoletos
        });

        var (combosList, subtotalCombos) = await AgregarCombosPreordenAsync(preorden.IdPreorden, dto.Combos);
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

    private static (string NombreLimpio, string TelefonoLimpio) ValidarSolicitudCrearPreorden(CrearPreordenDto dto)
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

        var telefonoLimpio = NormalizarTelefonoMexicano(dto.TelefonoCliente);

        return (nombreLimpio, telefonoLimpio);
    }

    private async Task<Cliente> ObtenerOActualizarClientePreordenAsync(string nombreLimpio, string telefonoLimpio)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.NumeroTelefono == telefonoLimpio);
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

        return cliente;
    }

    private async Task<string> GenerarFolioPreordenUnicoAsync()
    {
        string folio;
        do
        {
            var randomSuffix = RandomNumberGenerator.GetInt32(10000, 100000);
            folio = $"PRE-XIL-{randomSuffix}";
        } while (await _db.Preordens.AnyAsync(p => p.CodigoQr == folio));

        return folio;
    }

    private async Task<(List<PreordenComboDto> Combos, decimal Subtotal)> AgregarCombosPreordenAsync(
        int idPreorden,
        List<CrearPreordenItemDulceriaDto>? combosDto)
    {
        var combosList = new List<PreordenComboDto>();
        decimal subtotalCombos = 0m;

        if (combosDto == null || combosDto.Count == 0)
        {
            return (combosList, subtotalCombos);
        }

        foreach (var comboDto in combosDto.Where(c => c.IdProducto > 0 && c.Cantidad > 0))
        {
            var producto = await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == comboDto.IdProducto);
            if (producto == null) continue;

            _db.DetallePreordenDulceria.Add(new DetallePreordenDulcerium
            {
                IdPreorden = idPreorden,
                IdProducto = producto.IdProducto,
                Cantidad = comboDto.Cantidad
            });
            subtotalCombos += producto.PrecioVenta * comboDto.Cantidad;
            combosList.Add(new PreordenComboDto
            {
                Nombre = producto.Nombre,
                Cantidad = comboDto.Cantidad,
                Precio = producto.PrecioVenta
            });
        }

        return (combosList, subtotalCombos);
    }

    public async Task<SolicitarCodigoPreordenResponseDto> SolicitarCodigoRecuperacionAsync(SolicitarCodigoPreordenDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentException("El número de teléfono es obligatorio.");
        }

        var telefonoLimpio = NormalizarTelefonoMexicano(dto.Telefono);

        var tienePreordenes = await _db.Preordens
            .AnyAsync(p => p.IdClienteNavigation.NumeroTelefono == telefonoLimpio);

        if (!tienePreordenes)
        {
            throw new KeyNotFoundException("No encontramos pre-órdenes registradas con este número de teléfono.");
        }

        if (_otpStore.TryGetCooldownSeconds(telefonoLimpio, out var waitSeconds))
        {
            throw new TwilioSmsException(
                $"Ya enviamos un código recientemente. Espera {waitSeconds} segundos antes de solicitar uno nuevo.",
                429
            );
        }

        var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
        const int expiraMinutos = 10;
        _otpStore.SaveCode(telefonoLimpio, codigo, TimeSpan.FromMinutes(expiraMinutos));

        try
        {
            await EnviarSmsTwilioAsync(telefonoLimpio, codigo);
        }
        catch
        {
            _otpStore.RemoveCode(telefonoLimpio);
            throw;
        }

        var enmascarado = EnmascararTelefono(telefonoLimpio);
        return new SolicitarCodigoPreordenResponseDto
        {
            Enviado = true,
            TelefonoEnmascarado = enmascarado,
            ExpiraEnMinutos = expiraMinutos,
            Message = $"Enviamos un código de 6 dígitos por SMS al +52 {enmascarado}."
        };
    }

    public async Task<List<PreordenDto>> VerificarCodigoRecuperacionAsync(VerificarCodigoPreordenDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentException("Los datos de verificación son obligatorios.");
        }

        var telefonoLimpio = NormalizarTelefonoMexicano(dto.Telefono);
        var codigoLimpio = dto.Codigo?.Trim().Replace(" ", "").Replace("-", "") ?? string.Empty;

        if (codigoLimpio.Length != 6 || !codigoLimpio.All(char.IsDigit))
        {
            throw new ArgumentException("Ingresa el código de verificación de 6 dígitos.");
        }

        var validacion = _otpStore.ValidateCode(telefonoLimpio, codigoLimpio);
        if (validacion.Status == OtpValidationStatus.NotFoundOrExpired)
        {
            throw new ArgumentException("El código expiró o no ha sido solicitado. Solicita un código nuevo.");
        }
        if (validacion.Status == OtpValidationStatus.TooManyAttempts)
        {
            throw new ArgumentException("Superaste el máximo de intentos permitidos. Solicita un código nuevo.");
        }
        if (validacion.Status == OtpValidationStatus.InvalidCode)
        {
            throw new ArgumentException($"El código ingresado es incorrecto. Te quedan {validacion.RemainingAttempts} intento(s).");
        }

        var preordenes = await _db.Preordens
            .Include(p => p.IdClienteNavigation)
            .Include(p => p.IdFuncionNavigation.IdPeliculaNavigation)
            .Where(p => p.IdClienteNavigation.NumeroTelefono == telefonoLimpio)
            .OrderByDescending(p => p.FechaCreacion)
            .ThenByDescending(p => p.IdPreorden)
            .ToListAsync();

        var preordenIds = preordenes.Select(p => p.IdPreorden).ToList();
        if (preordenIds.Count == 0)
        {
            return new List<PreordenDto>();
        }

        var boletosPorPreorden = (await _db.DetallePreordenBoletos
            .Where(b => preordenIds.Contains(b.IdPreorden))
            .ToListAsync())
            .ToLookup(b => b.IdPreorden);

        var dulceriaPorPreorden = (await _db.DetallePreordenDulceria
            .Include(d => d.IdProductoNavigation)
            .Where(d => preordenIds.Contains(d.IdPreorden))
            .ToListAsync())
            .ToLookup(d => d.IdPreorden);

        return preordenes
            .Select(p => MapPreordenToDto(
                p,
                boletosPorPreorden[p.IdPreorden],
                dulceriaPorPreorden[p.IdPreorden],
                p.CodigoQr ?? string.Empty))
            .ToList();
    }

    private async Task EnviarSmsTwilioAsync(string telefono10Digitos, string codigo)
    {
        var accountSid = _config?["Twilio:AccountSid"]?.Trim() ?? string.Empty;
        var authToken = _config?["Twilio:AuthToken"]?.Trim() ?? string.Empty;
        var fromNumber = _config?["Twilio:FromNumber"]?.Trim() ?? string.Empty;
        var messagingServiceSid = _config?["Twilio:MessagingServiceSid"]?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(accountSid) ||
            string.IsNullOrEmpty(authToken) ||
            (string.IsNullOrEmpty(fromNumber) && string.IsNullOrEmpty(messagingServiceSid)))
        {
            throw new TwilioSmsException(
                "El servicio de envío por SMS (Twilio) aún no está configurado en el servidor.",
                503
            );
        }

        if (!accountSid.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            throw new TwilioSmsException("Configuración de Twilio inválida en el servidor.", 503);
        }

        var formFields = new List<KeyValuePair<string, string>>
        {
            new("To", $"+52{telefono10Digitos}"),
            new("Body", $"Cine Xilotzin: Tu codigo de verificacion para ver tus tickets es {codigo}. Valido por 10 minutos.")
        };

        if (!string.IsNullOrEmpty(messagingServiceSid))
        {
            formFields.Add(new("MessagingServiceSid", messagingServiceSid));
        }
        else
        {
            formFields.Add(new("From", fromNumber));
        }

        var requestUri = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new FormUrlEncodedContent(formFields)
        };

        var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new TwilioSmsException(
                "No se pudo conectar con el proveedor de SMS en este momento. Intenta de nuevo.",
                502
            );
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            throw BuildTwilioErrorException((int)response.StatusCode, responseBody);
        }
    }

    private static TwilioSmsException BuildTwilioErrorException(int httpStatusCode, string responseBody)
    {
        if (httpStatusCode == 401 || httpStatusCode == 403)
        {
            return new TwilioSmsException("Las credenciales de Twilio configuradas en el servidor son inválidas.", 503);
        }

        var twilioCode = TryExtractTwilioErrorCode(responseBody);
        if (twilioCode == 21608)
        {
            return new TwilioSmsException(
                "La cuenta de envío SMS está en modo prueba (Trial) y este número aún no está verificado en Twilio Console.",
                400
            );
        }
        if (twilioCode == 21211 || twilioCode == 21614)
        {
            return new TwilioSmsException("El número de teléfono no es válido para recibir SMS.", 400);
        }
        if (twilioCode == 21408)
        {
            return new TwilioSmsException("La región México (+52) aún no está habilitada para SMS en Twilio.", 503);
        }

        return new TwilioSmsException("No fue posible enviar el código SMS en este momento. Intenta de nuevo más tarde.", 502);
    }

    private static int? TryExtractTwilioErrorCode(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var code))
            {
                return code;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string NormalizarTelefonoMexicano(string? telefonoRaw)
    {
        var telefonoLimpio = telefonoRaw?.Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("+", "")
            .Replace("(", "")
            .Replace(")", "") ?? string.Empty;

        if (telefonoLimpio.StartsWith("521") && telefonoLimpio.Length == 13)
        {
            telefonoLimpio = telefonoLimpio.Substring(3);
        }
        else if (telefonoLimpio.StartsWith("52") && telefonoLimpio.Length == 12)
        {
            telefonoLimpio = telefonoLimpio.Substring(2);
        }

        if (telefonoLimpio.Length != 10 || !telefonoLimpio.All(char.IsDigit) || telefonoLimpio.StartsWith('0') || telefonoLimpio.StartsWith('1'))
        {
            throw new ArgumentException("El teléfono debe contener 10 dígitos válidos de México (ej. 7731234567 o 5512345678).");
        }

        return telefonoLimpio;
    }

    private static string EnmascararTelefono(string telefono10Digitos)
    {
        return telefono10Digitos.Length >= 4
            ? $"******{telefono10Digitos[^4..]}"
            : "******";
    }

    private static PreordenDto MapPreordenToDto(
        Preorden pre,
        IEnumerable<DetallePreordenBoleto> boletos,
        IEnumerable<DetallePreordenDulcerium> dulceria,
        string fallbackCodigo)
    {
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
            Codigo = pre.CodigoQr ?? fallbackCodigo,
            Cliente = pre.IdClienteNavigation?.Nombre ?? "Cliente Xilotzin",
            Telefono = pre.IdClienteNavigation?.NumeroTelefono ?? "",
            Pelicula = pre.IdFuncionNavigation?.IdPeliculaNavigation?.Titulo ?? "Película",
            Horario = pre.IdFuncionNavigation?.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "16:00",
            IdFuncion = pre.IdFuncion,
            BoletosGeneral = boletosCantidad,
            PrecioBoleto = precioBoleto,
            // La pre-orden no reserva butacas: se asignan en taquilla al canjear el QR (HU-06).
            Asientos = new List<string>(),
            Combos = combos,
            Total = subtotalBoletos + subtotalCombos,
            FechaCreacion = pre.FechaCreacion.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            VigenciaDias = pre.Vigencia,
            EstadoActivo = pre.EstadoActivo
        };
    }

    private static readonly ConcurrentDictionary<int, PeliculaTrailerDto> _trailersCache = new();

    private static readonly Dictionary<int, (string Key, string Nombre)> FallbackTrailers = new()
    {
        { 1, ("QXibcL7-XbU", "Spider-Man: Un Nuevo Día (Tráiler Final)") },
        { 2, ("I8aZahPFQnA", "Super Mario Bros. La Película (Tráiler Final)") },
        { 14, ("R4GEdGRQVBU", "Caminos del Crimen (Tráiler Oficial)") },
        { 15, ("cxTQ3aMYgfU", "Terrifier 3: Payaso Siniestro (Tráiler Final)") },
        { 16, ("az6bdokjRk8", "La Posesión de la Momia (Tráiler Oficial)") },
        { 17, ("M4ESyy4q9Qo", "Avatar: Fuego y Ceniza (Tráiler Oficial)") },
        { 18, ("YiWXTkpP9j0", "Five Nights at Freddy's 2 (Tráiler Oficial)") },
        { 19, ("ipzEY7c7it8", "Super Mario Galaxy (Tráiler Final)") },
        { 20, ("lAr_uspgHm8", "Avengers: Doomsday (Tráiler Oficial)") },
        { 21, ("9n7b0ORr-U4", "El Conjuro 4: Últimos Ritos (Tráiler Oficial)") }
    };

    private async Task<bool> EsVideoValidoEnYouTubeAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        try
        {
            var checkUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={Uri.EscapeDataString(key)}&format=json";
            using var req = new HttpRequestMessage(HttpMethod.Get, checkUrl);
            using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PeliculaTrailerDto?> ObtenerTrailerPeliculaAsync(int idPelicula)
    {
        if (_trailersCache.TryGetValue(idPelicula, out var cached) && !string.IsNullOrEmpty(cached.TrailerKey))
        {
            return cached;
        }

        var pelicula = await _db.Peliculas.FindAsync(idPelicula);
        if (pelicula == null) return null;

        var dto = new PeliculaTrailerDto
        {
            IdPelicula = pelicula.IdPelicula,
            Titulo = pelicula.Titulo
        };

        await PoblarVideosDesdeTmdbAsync(pelicula.Titulo, dto);
        AplicarFallbackTrailerSiNecesario(idPelicula, dto);

        if (!string.IsNullOrEmpty(dto.TrailerKey))
        {
            _trailersCache[idPelicula] = dto;
        }

        return dto;
    }

    private async Task PoblarVideosDesdeTmdbAsync(string tituloPelicula, PeliculaTrailerDto dto)
    {
        try
        {
            var tmdbId = await BuscarTmdbIdPorTituloConFallbackAsync(tituloPelicula);
            if (!tmdbId.HasValue) return;

            var items = await ObtenerVideosYoutubeDePeliculaTmdbAsync(tmdbId.Value);
            dto.Trailers = items;

            var ordenados = items
                .OrderBy(i => ObtenerPrioridadTipoVideo(i.Tipo))
                .ThenBy(i => ContieneNombreTrailerPrioritario(i.Nombre) ? 0 : 1)
                .ToList();

            dto.TrailerKey = await SeleccionarPrimerVideoValidoAsync(ordenados.Select(c => c.Key))
                             ?? await SeleccionarPrimerVideoValidoAsync(items.Select(c => c.Key));
        }
        catch
        {
            // Silencioso ante fallos de red hacia TMDB
        }
    }

    private async Task<int?> BuscarTmdbIdPorTituloConFallbackAsync(string titulo)
    {
        var tmdbId = await BuscarPrimerTmdbIdAsync(titulo.Trim());
        if (!tmdbId.HasValue && titulo.Contains(':'))
        {
            var cleanTitle = titulo.Split(':')[0].Trim();
            tmdbId = await BuscarPrimerTmdbIdAsync(cleanTitle);
        }
        return tmdbId;
    }

    private async Task<int?> BuscarPrimerTmdbIdAsync(string query)
    {
        var searchUrl = $"https://api.themoviedb.org/3/search/movie?api_key={_tmdbApiKey}&query={Uri.EscapeDataString(query)}&language=es-MX";
        var searchResponse = await _httpClient.GetAsync(searchUrl);
        if (!searchResponse.IsSuccessStatusCode) return null;

        var searchJson = await searchResponse.Content.ReadAsStringAsync();
        using var searchDoc = JsonDocument.Parse(searchJson);
        if (searchDoc.RootElement.TryGetProperty(PropiedadResults, out var results) && results.GetArrayLength() > 0)
        {
            return results[0].GetProperty("id").GetInt32();
        }
        return null;
    }

    private async Task<List<TrailerItemDto>> ObtenerVideosYoutubeDePeliculaTmdbAsync(int tmdbId)
    {
        var videosUrl = $"https://api.themoviedb.org/3/movie/{tmdbId}/videos?api_key={_tmdbApiKey}&include_video_language=es,es-MX,es-ES,en,null";
        var videosResponse = await _httpClient.GetAsync(videosUrl);
        var videosJson = videosResponse.IsSuccessStatusCode ? await videosResponse.Content.ReadAsStringAsync() : "{}";
        using var videosDoc = JsonDocument.Parse(videosJson);

        if (!videosDoc.RootElement.TryGetProperty(PropiedadResults, out var vidArray) || vidArray.GetArrayLength() == 0)
        {
            return new List<TrailerItemDto>();
        }

        return vidArray.EnumerateArray()
            .Where(v => GetJsonString(v, "site") == "YouTube" && !string.IsNullOrWhiteSpace(GetJsonString(v, "key")))
            .Select(v => new TrailerItemDto
            {
                Key = GetJsonString(v, "key"),
                Nombre = GetJsonString(v, "name"),
                Tipo = GetJsonString(v, "type")
            })
            .ToList();
    }

    private static int ObtenerPrioridadTipoVideo(string tipo)
    {
        if (tipo == TipoVideoTrailer) return 0;
        if (tipo == "Teaser") return 1;
        return 2;
    }

    private static bool ContieneNombreTrailerPrioritario(string nombre) =>
        nombre.Contains("Español", StringComparison.OrdinalIgnoreCase) ||
        nombre.Contains("Doblado", StringComparison.OrdinalIgnoreCase) ||
        nombre.Contains("Tráiler", StringComparison.OrdinalIgnoreCase) ||
        nombre.Contains(TipoVideoTrailer, StringComparison.OrdinalIgnoreCase);

    private async Task<string?> SeleccionarPrimerVideoValidoAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            if (await EsVideoValidoEnYouTubeAsync(key))
            {
                return key;
            }
        }
        return null;
    }

    private static void AplicarFallbackTrailerSiNecesario(int idPelicula, PeliculaTrailerDto dto)
    {
        if (!string.IsNullOrEmpty(dto.TrailerKey) || !FallbackTrailers.TryGetValue(idPelicula, out var fb))
        {
            return;
        }

        dto.TrailerKey = fb.Key;
        if (dto.Trailers.Count == 0)
        {
            dto.Trailers.Add(new TrailerItemDto
            {
                Key = fb.Key,
                Nombre = fb.Nombre,
                Tipo = TipoVideoTrailer
            });
        }
    }
}
