namespace cine_back.Dtos
{
    public class NetworkCheckResponseDto
    {
        public bool IsAllowed { get; set; }
        public string IpDetected { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
