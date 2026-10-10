using cine_back.Data;
using cine_back.Dtos;
using cine_back.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace cine_back.Services
{
    public class AuthService : IAuthService
    {
        private readonly CineDbContext _db;

    public AuthService(CineDbContext db)
        {
            _db = db;
        }

        public async Task<NetworkCheckResponseDto> CheckNetworkAsync(string clientIp)
        {
            clientIp = ResolveClientIp(clientIp);

            bool isAuthorized = await _db.RedesAutorizadas
                .AnyAsync(r => r.DireccionIp == clientIp);

            if (!isAuthorized)
            {
                return new NetworkCheckResponseDto
                {
                    IsAllowed = false,
                    IpDetected = clientIp,
                    Message = $"BLOQUEO POR RESTRICCION DE RED (IP EXTERNA). IP detectada: {clientIp}"
                };
            }

            return new NetworkCheckResponseDto
            {
                IsAllowed = true,
                IpDetected = clientIp,
                Message = "Red Autorizada (Red Local del Cine)"
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string clientIp)
        {
            // 1. Resolver IP si viene de Swagger (localhost)
            clientIp = ResolveClientIp(clientIp);

            // TODO: [Deuda Técnica] Eliminar este usuario administrador hardcodeado 
            // una vez que la HU de Registro de Usuarios esté terminada y la BD tenga datos.
            if (request.NombreUsuario == "admin" && request.Password == "admin123")
            {
                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Inicio de sesión exitoso (Admin temporal).",
                    IdVendedor = 0,
                    Nombre = "Administrador Supremo",
                    Rol = "Admin" // Como el rol no es "Vendedor", no le afectará el bloqueo de IP
                };
            }
            // ====================================================================

            // 2. Buscar al vendedor por nombre en la Base de Datos
            var vendedor = await _db.Vendedors
                .Include(v => v.IdRolNavigation)
                .FirstOrDefaultAsync(v => v.Nombre == request.NombreUsuario && v.EstadoActivo);

            if (vendedor == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Usuario o contraseña incorrectos."
                };
            }

            // 3. PRIMERO: Comparar contraseña encriptada (Seguridad básica)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, vendedor.PasswordHash);

            if (!isPasswordValid)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Usuario o contraseña incorrectos."
                };
            }

            // 4. SEGUNDO: Verificar restricción de IP de la HU-04 SOLAMENTE para el rol Vendedor[cite: 12]
            bool isLocalIp = await _db.RedesAutorizadas.AnyAsync(r => r.DireccionIp == clientIp);
            string nombreRol = vendedor.IdRolNavigation?.NombreRol ?? string.Empty;

            if (nombreRol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase) && !isLocalIp)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = $"El rol Vendedor solo tiene autorización de operar conectado a la red local del cine. IP detectada: {clientIp}."
                };
            }

            // 5. Login Exitoso para usuarios reales
            return new LoginResponseDto
            {
                Success = true,
                Message = "Inicio de sesión exitoso.",
                IdVendedor = vendedor.IdVendedor,
                Nombre = vendedor.Nombre,
                Rol = nombreRol
            };
        }

        /// Resuelve dinámicamente la IP física del adaptador de red (solo para entorno de desarrollo local).
        /// Permite que las pruebas en Swagger (localhost / ::1) sean validadas contra la BD.
        private string ResolveClientIp(string clientIp)
        {
            // Si la petición viene de la misma máquina, buscamos la IP del adaptador de red que empiece con 192.168.
            // (Esto evita que tome IPs virtuales de Docker/WSL2 tipo 172.x.x.x)
            if (clientIp == "::1" || clientIp == "127.0.0.1")
            {
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        {
                            string ipStr = ip.ToString();
                            // Priorizar la red local estándar si existe
                            if (ipStr.StartsWith("192.168."))
                            {
                                return ipStr;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignorar errores de resolución DNS
                }
            }

            return clientIp;
        }
    }
}

