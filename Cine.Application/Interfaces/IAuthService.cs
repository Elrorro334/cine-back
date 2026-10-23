using Cine.Application.Dtos;

namespace Cine.Application.Interfaces
{
    public interface IAuthService
    {
        Task<NetworkCheckResponseDto> CheckNetworkAsync(string clientIp);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string clientIp);
    }
}
