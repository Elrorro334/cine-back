using cine_back.Dtos;

namespace cine_back.Infrastructure
{
    public interface IAuthService
    {
        Task<NetworkCheckResponseDto> CheckNetworkAsync(string clientIp);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string clientIp);
    }
}
