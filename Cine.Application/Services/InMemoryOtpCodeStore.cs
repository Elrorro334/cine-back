using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Cine.Application.Interfaces;

namespace Cine.Application.Services;

public class InMemoryOtpCodeStore : IOtpCodeStore
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<string, OtpEntry> _entries = new();
    private readonly TimeProvider _timeProvider;

    public InMemoryOtpCodeStore() : this(TimeProvider.System)
    {
    }

    public InMemoryOtpCodeStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool TryGetCooldownSeconds(string telefono, out int secondsRemaining)
    {
        secondsRemaining = 0;
        if (!_entries.TryGetValue(telefono, out var entry))
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        lock (entry)
        {
            if (now >= entry.ExpiresAtUtc)
            {
                _entries.TryRemove(telefono, out _);
                return false;
            }

            var elapsed = now - entry.LastSentAtUtc;
            if (elapsed < ResendCooldown)
            {
                secondsRemaining = Math.Max(1, (int)Math.Ceiling((ResendCooldown - elapsed).TotalSeconds));
                return true;
            }
        }

        return false;
    }

    public void SaveCode(string telefono, string codigo, TimeSpan ttl, int maxAttempts = 5)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        CleanupExpired(now);

        var hash = ComputeHash(telefono, codigo);
        _entries[telefono] = new OtpEntry(hash, now.Add(ttl), now, maxAttempts);
    }

    public void RemoveCode(string telefono)
    {
        _entries.TryRemove(telefono, out _);
    }

    public OtpValidationResult ValidateCode(string telefono, string codigo)
    {
        if (!_entries.TryGetValue(telefono, out var entry))
        {
            return new OtpValidationResult(OtpValidationStatus.NotFoundOrExpired, 0);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidateHash = ComputeHash(telefono, codigo);

        lock (entry)
        {
            if (now >= entry.ExpiresAtUtc)
            {
                _entries.TryRemove(telefono, out _);
                return new OtpValidationResult(OtpValidationStatus.NotFoundOrExpired, 0);
            }

            if (entry.RemainingAttempts <= 0)
            {
                _entries.TryRemove(telefono, out _);
                return new OtpValidationResult(OtpValidationStatus.TooManyAttempts, 0);
            }

            if (CryptographicOperations.FixedTimeEquals(entry.CodeHash, candidateHash))
            {
                _entries.TryRemove(telefono, out _);
                return new OtpValidationResult(OtpValidationStatus.Success, entry.RemainingAttempts);
            }

            entry.RemainingAttempts--;
            if (entry.RemainingAttempts <= 0)
            {
                _entries.TryRemove(telefono, out _);
                return new OtpValidationResult(OtpValidationStatus.TooManyAttempts, 0);
            }

            return new OtpValidationResult(OtpValidationStatus.InvalidCode, entry.RemainingAttempts);
        }
    }

    private void CleanupExpired(DateTime utcNow)
    {
        foreach (var pair in _entries)
        {
            if (utcNow >= pair.Value.ExpiresAtUtc)
            {
                _entries.TryRemove(pair.Key, out _);
            }
        }
    }

    private static byte[] ComputeHash(string telefono, string codigo)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes($"{telefono}:{codigo}"));
    }

    private sealed class OtpEntry
    {
        public byte[] CodeHash { get; }
        public DateTime ExpiresAtUtc { get; }
        public DateTime LastSentAtUtc { get; }
        public int RemainingAttempts { get; set; }

        public OtpEntry(byte[] codeHash, DateTime expiresAtUtc, DateTime lastSentAtUtc, int remainingAttempts)
        {
            CodeHash = codeHash;
            ExpiresAtUtc = expiresAtUtc;
            LastSentAtUtc = lastSentAtUtc;
            RemainingAttempts = remainingAttempts;
        }
    }
}
