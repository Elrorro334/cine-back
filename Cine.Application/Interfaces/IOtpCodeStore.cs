namespace Cine.Application.Interfaces;

public enum OtpValidationStatus
{
    Success = 0,
    NotFoundOrExpired = 1,
    TooManyAttempts = 2,
    InvalidCode = 3
}

public readonly record struct OtpValidationResult(OtpValidationStatus Status, int RemainingAttempts);

public interface IOtpCodeStore
{
    bool TryGetCooldownSeconds(string telefono, out int secondsRemaining);
    void SaveCode(string telefono, string codigo, TimeSpan ttl, int maxAttempts = 5);
    void RemoveCode(string telefono);
    OtpValidationResult ValidateCode(string telefono, string codigo);
}
