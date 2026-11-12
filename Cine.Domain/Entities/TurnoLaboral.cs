using System;

namespace Cine.Domain.Entities;

public partial class TurnoLaboral
{
    public int Id { get; set; }

    public string NombreTurno { get; set; } = null!;

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }
}
