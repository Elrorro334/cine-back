using System;

namespace Cine.Domain.Entities;

public partial class ConfiguracionSeguridad
{
    public int Id { get; set; }

    public int SucursalId { get; set; }

    public string IpAutorizada { get; set; } = null!;

    public TimeSpan HorarioInicioCajeros { get; set; }

    public TimeSpan HorarioFinCajeros { get; set; }

    public bool AutorizarExcepcionHorasExtras { get; set; }

    public DateTime FechaUltimaModificacion { get; set; }

    public int UsuarioModificoId { get; set; }

    public virtual Sucursal SucursalNavigation { get; set; } = null!;
}
