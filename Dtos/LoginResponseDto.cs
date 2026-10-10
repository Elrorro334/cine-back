namespace cine_back.Dtos
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdVendedor { get; set; }
        public string? Nombre { get; set; }
        public string? Rol { get; set; }
    }
}
