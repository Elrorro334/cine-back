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
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={_tmdbApiKey}&language=es-MX&append_to_response=release_dates,credits,videos";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : tmdbId;
            var titulo = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
            var original = root.TryGetProperty("original_title", out var origProp) ? origProp.GetString() ?? "" : "";
            var tagline = root.TryGetProperty("tagline", out var tagProp) ? tagProp.GetString() : null;
            var eslogan = !string.IsNullOrWhiteSpace(tagline) ? tagline : "Cine Tradicional en Jilotepec";

            var overview = root.TryGetProperty("overview", out var overProp) ? overProp.GetString() : null;
            var sinopsis = !string.IsNullOrWhiteSpace(overview)
                ? overview
                : "Disfruta de esta emocionante producción en nuestra pantalla gigante con audio cinematográfico inmersivo.";

            var runtime = root.TryGetProperty("runtime", out var runProp) && runProp.ValueKind == JsonValueKind.Number ? runProp.GetInt32() : 120;
            var poster = root.TryGetProperty("poster_path", out var posterProp) ? posterProp.GetString() ?? "" : "";
            var backdrop = root.TryGetProperty("backdrop_path", out var backProp) ? backProp.GetString() ?? "" : "";
            var release = root.TryGetProperty("release_date", out var relProp) ? relProp.GetString() ?? "" : "";
            var voteAvg = root.TryGetProperty("vote_average", out var voteProp) && voteProp.ValueKind == JsonValueKind.Number ? voteProp.GetDouble() : 8.5;

            var year = !string.IsNullOrEmpty(release) && release.Length >= 4 ? release.Substring(0, 4) : DateTime.Now.Year.ToString();

            // Géneros (Principal y Lista Completa)
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
            var genero = generosList.Count > 0 ? generosList[0] : "Cine";
            var generosCompletos = generosList.Count > 0 ? string.Join(", ", generosList) : "Acción, Aventuras";

            // Créditos: Director y Actores (Top 3)
            var director = "Director Cinematográfico";
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
                        if (count >= 3) break;
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
            var actores = actoresList.Count > 0 ? string.Join(", ", actoresList) : "Elenco estelar";

            // Certificación / Clasificación (Mapeo estricto: R -> C, PG-13 -> B15, PG -> B, G -> AA, NC-17 -> C)
            var mapaCert = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

            var clasificacion = "B";
            if (root.TryGetProperty("release_dates", out var rdProp) &&
                rdProp.TryGetProperty("results", out var rdResults) &&
                rdResults.ValueKind == JsonValueKind.Array)
            {
                JsonElement? mxRelease = null;
                JsonElement? usRelease = null;

                foreach (var country in rdResults.EnumerateArray())
                {
                    var iso = country.TryGetProperty("iso_3166_1", out var isoProp) ? isoProp.GetString() : "";
                    if (iso == "MX" && mxRelease == null) mxRelease = country;
                    if (iso == "US" && usRelease == null) usRelease = country;
                }

                var targetRelease = mxRelease ?? usRelease;
                if (targetRelease.HasValue &&
                    targetRelease.Value.TryGetProperty("release_dates", out var datesArr) &&
                    datesArr.ValueKind == JsonValueKind.Array &&
                    datesArr.GetArrayLength() > 0)
                {
                    foreach (var cd in datesArr.EnumerateArray())
                    {
                        var cert = cd.TryGetProperty("certification", out var certProp) ? certProp.GetString() : "";
                        if (!string.IsNullOrWhiteSpace(cert))
                        {
                            if (mapaCert.TryGetValue(cert.Trim(), out var certMapeada))
                            {
                                clasificacion = certMapeada;
                            }
                            else
                            {
                                clasificacion = cert.Trim();
                            }
                            break;
                        }
                    }
                }
            }

            // Tráiler Key de YouTube
            string? trailerKey = null;
            if (root.TryGetProperty("videos", out var videosProp) &&
                videosProp.TryGetProperty("results", out var vidResults) &&
                vidResults.ValueKind == JsonValueKind.Array)
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

        // 5. Crear Funciones para el rango de fechas y horarios configurados
        var fechaInicio = dto.FechaInicio?.Date ?? DateTime.Today;
        var fechaFin = dto.FechaFin?.Date ?? fechaInicio;
        if (fechaFin < fechaInicio)
        {
            fechaFin = fechaInicio;
        }

        // Limitar rango a maximo 60 dias por seguridad
        if ((fechaFin - fechaInicio).TotalDays > 60)
        {
            fechaFin = fechaInicio.AddDays(60);
        }

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

        var funcionesParaPelicula = new List<Funcion>();

        for (var dia = fechaInicio; dia <= fechaFin; dia = dia.AddDays(1))
        {
            foreach (var hStr in listaHorarios)
            {
                if (!TimeSpan.TryParse(hStr, out var timeSpan))
                {
                    continue;
                }

                var horaInicio = dia.Add(timeSpan);

                var funcionExistente = await _db.Funcions.FirstOrDefaultAsync(f =>
                    f.IdPelicula == pelicula.IdPelicula &&
                    f.IdSala == sala.IdSala &&
                    f.FechaFuncion.Date == dia.Date &&
                    f.HoraInicio == horaInicio);

                if (funcionExistente == null)
                {
                    var nuevaFuncion = new Funcion
                    {
                        IdPelicula = pelicula.IdPelicula,
                        IdSala = sala.IdSala,
                        FechaFuncion = dia.Date,
                        HoraInicio = horaInicio,
                        Precio = dto.Precio > 0 ? dto.Precio : 50.00m
                    };
                    _db.Funcions.Add(nuevaFuncion);
                    funcionesParaPelicula.Add(nuevaFuncion);
                }
                else
                {
                    funcionesParaPelicula.Add(funcionExistente);
                }
            }
        }

        await _db.SaveChangesAsync();

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
            TmdbMovieDetailDto? detalle = null;
            try
            {
                var resultados = await BuscarTmdbAsync(item.Titulo);
                if (resultados != null && resultados.Count > 0)
                {
                    detalle = await ObtenerDetalleTmdbAsync(resultados[0].TmdbId);
                }
            }
            catch
            {
                // Ignorar error de red y usar fallback
            }

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

    public async Task<List<PromocionDto>> ObtenerPromocionesAsync()
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

        return await Task.FromResult(promociones);
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

        var imagenes = new Dictionary<string, string>
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

        return productos.Select(p => new ProductoDto
        {
            IdProducto = p.IdProducto,
            Sku = p.CodigoBarras,
            Nombre = p.Nombre,
            Categoria = p.IdCategoriaNavigation?.Nombre ?? "Snacks",
            Precio = p.PrecioVenta,
            Stock = p.StockActual,
            Img = imagenes.TryGetValue(p.CodigoBarras, out var imgUrl) ? imgUrl : "https://images.unsplash.com/photo-1585647347384-2593bc35786b?q=80&w=400"
        }).ToList();
    }

    public async Task<VentaConfirmadaDto> RegistrarVentaAsync(RegistrarVentaDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("No se pueden registrar ventas sin artículos.");
        }

        var totalCalculado = dto.Items.Sum(i => i.Precio * i.Cantidad);

        var venta = new Ventum
        {
            IdVendedor = dto.IdVendedor > 0 ? dto.IdVendedor : 1,
            FechaVenta = DateTime.Now,
            Total = totalCalculado,
            MetodoPago = string.IsNullOrWhiteSpace(dto.MetodoPago) ? "Efectivo" : dto.MetodoPago
        };

        _db.Venta.Add(venta);
        await _db.SaveChangesAsync();

        var productoBoleto = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "BOL-GEN");

        foreach (var item in dto.Items)
        {
            if (item.Tipo == "dulceria" || item.IdProducto.HasValue)
            {
                var producto = item.IdProducto.HasValue
                    ? await _db.Productos.FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto.Value)
                    : await _db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == item.Sku || p.Nombre == item.Nombre);

                if (producto != null)
                {
                    var detalle = new DetalleVentum
                    {
                        IdVenta = venta.IdVenta,
                        IdProducto = producto.IdProducto,
                        Cantidad = item.Cantidad
                    };
                    _db.DetalleVenta.Add(detalle);

                    producto.StockActual = Math.Max(0, producto.StockActual - item.Cantidad);
                }
            }
            else if (item.Tipo == "boleto")
            {
                if (productoBoleto != null)
                {
                    var detalle = new DetalleVentum
                    {
                        IdVenta = venta.IdVenta,
                        IdProducto = productoBoleto.IdProducto,
                        Cantidad = item.Cantidad
                    };
                    _db.DetalleVenta.Add(detalle);
                }

                if (item.IdFuncion.HasValue && item.Asientos != null && item.Asientos.Count > 0)
                {
                    var funcionId = item.IdFuncion.Value;
                    var funcion = await _db.Funcions.FirstOrDefaultAsync(f => f.IdFuncion == funcionId);
                    var idSala = funcion?.IdSala ?? 1;

                    foreach (var asientoCodigo in item.Asientos)
                    {
                        if (string.IsNullOrWhiteSpace(asientoCodigo) || asientoCodigo.Length < 2) continue;
                        var fila = asientoCodigo.Substring(0, 1);
                        if (!int.TryParse(asientoCodigo.Substring(1), out var numero)) continue;

                        var butaca = await _db.Butacas
                            .FirstOrDefaultAsync(b => b.IdSala == idSala && b.Fila == fila && b.Numero == numero);

                        if (butaca != null)
                        {
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
                    }
                }
            }
        }

        await _db.SaveChangesAsync();

        return new VentaConfirmadaDto
        {
            IdVenta = venta.IdVenta,
            Folio = $"VT-{venta.IdVenta:D5}",
            Total = venta.Total,
            MetodoPago = venta.MetodoPago ?? "Efectivo",
            FechaVenta = venta.FechaVenta,
            Items = dto.Items
        };
    }

    public async Task<PreordenDto?> ObtenerPreordenAsync(string codigoQr)
    {
        if (string.IsNullOrWhiteSpace(codigoQr)) return null;

        var pre = await _db.Preordens
            .Include(p => p.IdClienteNavigation)
            .Include(p => p.IdFuncionNavigation)
                .ThenInclude(f => f.IdPeliculaNavigation)
            .Include(p => p.DetallePreordenBoletos)
            .Include(p => p.DetallePreordenDulceria)
                .ThenInclude(d => d.IdProductoNavigation)
            .FirstOrDefaultAsync(p => p.CodigoQr == codigoQr);

        if (pre == null) return null;

        var boletosCantidad = pre.DetallePreordenBoletos.Sum(b => b.Caantidad);
        var precioBoleto = pre.IdFuncionNavigation?.Precio ?? 50m;
        var subtotalBoletos = boletosCantidad * precioBoleto;

        var combos = pre.DetallePreordenDulceria.Select(d => new PreordenComboDto
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
            Horario = pre.IdFuncionNavigation?.HoraInicio.ToString("HH:mm") ?? "16:00",
            IdFuncion = pre.IdFuncion,
            BoletosGeneral = boletosCantidad,
            PrecioBoleto = precioBoleto,
            Asientos = new List<string> { "C4", "C5" },
            Combos = combos,
            Total = subtotalBoletos + subtotalCombos,
            FechaCreacion = pre.FechaCreacion.ToString("yyyy-MM-dd"),
            VigenciaDias = pre.Vigencia,
            EstadoActivo = pre.EstadoActivo
        };
    }

}