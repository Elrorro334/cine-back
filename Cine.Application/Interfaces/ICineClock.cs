using System;

namespace Cine.Application.Interfaces;

public interface ICineClock
{
    DateTime GetLocalNow();
    TimeSpan GetLocalTimeOfDay();
}
