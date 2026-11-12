using System.Net;
using System.Net.Sockets;

namespace Cine.Application.Helpers;

public static class IpAddressHelper
{
    private const string DefaultLoopback = "127.0.0.1";

    public static string ExtractClientIp(string? forwardedFor, IPAddress? remoteIpAddress)
    {
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstIp = forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstIp))
            {
                return NormalizeIp(StripPort(firstIp));
            }
        }

        if (remoteIpAddress != null)
        {
            return NormalizeIp(remoteIpAddress.ToString());
        }

        return DefaultLoopback;
    }

    public static bool TryParseNormalized(string? ipText, out IPAddress? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(ipText))
        {
            return false;
        }

        var trimmed = ipText.Trim();
        if (!IPAddress.TryParse(trimmed, out var raw))
        {
            return false;
        }

        parsed = raw.IsIPv4MappedToIPv6 ? raw.MapToIPv4() : raw;
        return true;
    }

    public static string NormalizeIp(string? ipText)
    {
        if (string.IsNullOrWhiteSpace(ipText))
        {
            return DefaultLoopback;
        }

        if (TryParseNormalized(ipText, out var parsed) && parsed != null)
        {
            return parsed.ToString();
        }

        return ipText.Trim();
    }

    public static string ResolveLocalAdapterIfLoopback(string? ipText)
    {
        var normalized = NormalizeIp(ipText);
        if (normalized != "::1" && normalized != DefaultLoopback)
        {
            return normalized;
        }

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    string ipStr = ip.ToString();
                    if (ipStr.StartsWith("192.168.", StringComparison.Ordinal))
                    {
                        return ipStr;
                    }
                }
            }
        }
        catch
        {
            // Ignorar errores de resolución DNS en entornos sin red
        }

        return normalized;
    }

    public static bool AreSameIp(string? ipA, string? ipB)
    {
        if (TryParseNormalized(ipA, out var parsedA) &&
            TryParseNormalized(ipB, out var parsedB) &&
            parsedA != null &&
            parsedB != null)
        {
            return parsedA.Equals(parsedB);
        }

        return string.Equals(ipA?.Trim(), ipB?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSameSubnet(string? clientIpText, string? targetIpText)
    {
        if (!TryParseNormalized(clientIpText, out var clientIp) ||
            !TryParseNormalized(targetIpText, out var targetIp) ||
            clientIp == null ||
            targetIp == null)
        {
            return false;
        }

        if (clientIp.AddressFamily != targetIp.AddressFamily)
        {
            return false;
        }

        var clientBytes = clientIp.GetAddressBytes();
        var targetBytes = targetIp.GetAddressBytes();

        if (clientIp.AddressFamily == AddressFamily.InterNetwork)
        {
            // Subred IPv4 /24: coinciden los primeros 3 octetos
            return clientBytes[0] == targetBytes[0]
                && clientBytes[1] == targetBytes[1]
                && clientBytes[2] == targetBytes[2];
        }

        if (clientIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Prefijo IPv6 /64: coinciden los primeros 8 bytes
            for (int i = 0; i < 8; i++)
            {
                if (clientBytes[i] != targetBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    private static string StripPort(string rawValue)
    {
        var trimmed = rawValue.Trim();

        if (trimmed.StartsWith('['))
        {
            var closingIndex = trimmed.IndexOf(']');
            if (closingIndex > 1)
            {
                return trimmed[1..closingIndex];
            }
        }

        // Si es IPv4 con puerto (un solo ':')
        var firstColon = trimmed.IndexOf(':');
        if (firstColon > 0 && trimmed.IndexOf(':', firstColon + 1) == -1)
        {
            return trimmed[..firstColon];
        }

        return trimmed;
    }
}
