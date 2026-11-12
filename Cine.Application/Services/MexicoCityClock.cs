using System;
using Cine.Application.Interfaces;

namespace Cine.Application.Services;

public class MexicoCityClock : ICineClock
{
    private static readonly TimeZoneInfo MexicoCityTimeZone = ResolveMexicoCityTimeZone();

    public DateTime GetLocalNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MexicoCityTimeZone);
    }

    public TimeSpan GetLocalTimeOfDay()
    {
        return GetLocalNow().TimeOfDay;
    }

    private static TimeZoneInfo ResolveMexicoCityTimeZone()
    {
        foreach (var id in new[] { "America/Mexico_City", "Central Standard Time (Mexico)" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Intentar siguiente identificador multiplataforma
            }
            catch (InvalidTimeZoneException)
            {
                // Intentar siguiente identificador multiplataforma
            }
        }

        return TimeZoneInfo.Local;
    }
}
