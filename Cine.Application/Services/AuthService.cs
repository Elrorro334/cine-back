using Cine.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Cine.Application.Interfaces;
using Cine.Application.Dtos;
using Cine.Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Services
{
    public class AuthService : IAuthService
    {
        private const string RolAdministrador = "Administrador";
        private const string RolAdminCorto = "Admin";
        private const string RolVendedor = "Vendedor";
        private const string RolCajero = "Cajero";
        private const string MensajeCredencialesInvalidas = "Usuario o contraseña incorrectos.";
        private const string MensajeFueraDeHorario = "Acceso fuera de horario laboral autorizado";

        private readonly ICineDbContext _db;
        private readonly IConfiguration _config;
        private readonly ICineClock _clock;

        public AuthService(ICineDbContext db, IConfiguration configuration)
            : this(db, configuration, new MexicoCityClock())
        {
        }

        [ActivatorUtilitiesConstructor]
        public AuthService(ICineDbContext db, IConfiguration configuration, ICineClock clock)
        {
            _db = db;
            _config = configuration;
            _clock = clock;
        }

        public async Task<NetworkCheckResponseDto> CheckNetworkAsync(string clientIp)
        {
            string rawNormalized = IpAddressHelper.NormalizeIp(clientIp);
            string resolvedIp = IpAddressHelper.ResolveLocalAdapterIfLoopback(rawNormalized);

            var configSeguridad = await _db.ConfiguracionesSeguridad
                .AsNoTracking()
                .OrderByDescending(c => c.FechaUltimaModificacion)
                .FirstOrDefaultAsync();

            bool isAuthorized = await ValidarIpAutorizadaAsync(configSeguridad, rawNormalized, resolvedIp);

            if (!isAuthorized)
            {
                return new NetworkCheckResponseDto
                {
                    IsAllowed = false,
                    IpDetected = resolvedIp,
                    Message = $"BLOQUEO POR RESTRICCION DE RED (IP EXTERNA). IP detectada: {resolvedIp}"
                };
            }

            return new NetworkCheckResponseDto
            {
                IsAllowed = true,
                IpDetected = resolvedIp,
                Message = "Red Autorizada (Red Local del Cine)"
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string clientIp)
        {
            string rawNormalized = IpAddressHelper.NormalizeIp(clientIp);
            string resolvedIp = IpAddressHelper.ResolveLocalAdapterIfLoopback(rawNormalized);

            // Usuario administrador de rescate (autodestructible cuando existe admin real en BD)
            bool existeAdminReal = await _db.Vendedors
                .AnyAsync(v => (v.IdRolNavigation.NombreRol == RolAdminCorto ||
                                v.IdRolNavigation.NombreRol == RolAdministrador) && v.EstadoActivo);

            if (!existeAdminReal && request.NombreUsuario == "admin" && request.Password == "admin123")
            {
                var adminDummy = new Vendedor
                {
                    IdVendedor = 1,
                    Nombre = "Administrador Supremo",
                    EstadoActivo = true
                };
                string tokenAdmin = GenerarTokenJwt(adminDummy, RolAdminCorto);

                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Inicio de sesión exitoso (Admin de rescate habilitado).",
                    IdVendedor = 0,
                    Nombre = "Administrador Supremo",
                    Rol = RolAdminCorto,
                    Token = tokenAdmin
                };
            }

            var vendedor = await _db.Vendedors
                .Include(v => v.IdRolNavigation)
                .FirstOrDefaultAsync(v => v.Nombre == request.NombreUsuario && v.EstadoActivo);

            if (vendedor == null || !BCrypt.Net.BCrypt.Verify(request.Password, vendedor.PasswordHash))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = MensajeCredencialesInvalidas
                };
            }

            string nombreRol = vendedor.IdRolNavigation?.NombreRol ?? string.Empty;

            // Regla 1 (Bypass Admin): Omitir todas las restricciones de IP y horario
            if (!EsRolAdministrador(nombreRol) && EsRolOperativoRestringido(nombreRol))
            {
                var rechazoOperativo = await ValidarAccesoOperativoAsync(rawNormalized, resolvedIp);
                if (rechazoOperativo != null)
                {
                    return rechazoOperativo;
                }
            }

            string tokenGenerado = GenerarTokenJwt(vendedor, nombreRol);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Inicio de sesión exitoso.",
                IdVendedor = vendedor.IdVendedor,
                Nombre = vendedor.Nombre,
                Rol = nombreRol,
                Token = tokenGenerado
            };
        }

        private async Task<LoginResponseDto?> ValidarAccesoOperativoAsync(string rawNormalized, string resolvedIp)
        {
            var configSeguridad = await _db.ConfiguracionesSeguridad
                .AsNoTracking()
                .OrderByDescending(c => c.FechaUltimaModificacion)
                .FirstOrDefaultAsync();

            // Regla 2 (Validación IP Cajero / Vendedor)
            bool isLocalIp = await ValidarIpAutorizadaAsync(configSeguridad, rawNormalized, resolvedIp);
            if (!isLocalIp)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = $"Bloqueo por restricción de red: El rol Vendedor solo tiene autorización de operar conectado a la red local del cine. IP detectada: {resolvedIp}."
                };
            }

            // Regla 3 (Validación Horario Cajero / Vendedor y Excepción de Horas Extras)
            if (configSeguridad != null && !configSeguridad.AutorizarExcepcionHorasExtras)
            {
                var horaActual = _clock.GetLocalTimeOfDay();
                if (!EstaDentroDeHorario(horaActual, configSeguridad.HorarioInicioCajeros, configSeguridad.HorarioFinCajeros))
                {
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = MensajeFueraDeHorario
                    };
                }
            }

            return null;
        }

        private async Task<bool> ValidarIpAutorizadaAsync(
            ConfiguracionSeguridad? configSeguridad,
            string rawNormalized,
            string resolvedIp)
        {
            if (configSeguridad != null)
            {
                return IpAddressHelper.AreSameIp(resolvedIp, configSeguridad.IpAutorizada) ||
                       IpAddressHelper.AreSameIp(rawNormalized, configSeguridad.IpAutorizada);
            }

            var redes = await _db.RedesAutorizadas
                .AsNoTracking()
                .Select(r => r.DireccionIp)
                .ToListAsync();

            return redes.Any(ip =>
                IpAddressHelper.AreSameIp(resolvedIp, ip) ||
                IpAddressHelper.AreSameIp(rawNormalized, ip));
        }

        private static bool EsRolAdministrador(string nombreRol)
        {
            return nombreRol.Equals(RolAdministrador, StringComparison.OrdinalIgnoreCase) ||
                   nombreRol.Equals(RolAdminCorto, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsRolOperativoRestringido(string nombreRol)
        {
            return nombreRol.Equals(RolVendedor, StringComparison.OrdinalIgnoreCase) ||
                   nombreRol.Equals(RolCajero, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EstaDentroDeHorario(TimeSpan actual, TimeSpan inicio, TimeSpan fin)
        {
            if (inicio <= fin)
            {
                return actual >= inicio && actual <= fin;
            }

            // Soporte para horarios nocturnos que cruzan la medianoche (ej. 18:00 - 02:00)
            return actual >= inicio || actual <= fin;
        }

        private string GenerarTokenJwt(Vendedor vendedor, string nombreRol)
        {
            var secretKey = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException("La clave JWT no está configurada en appsettings.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, vendedor.IdVendedor.ToString()),
                    new Claim(ClaimTypes.Name, vendedor.Nombre),
                    new Claim(ClaimTypes.Role, nombreRol)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes), // NOSONAR
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
