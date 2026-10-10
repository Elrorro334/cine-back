using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Butaca
{
    public int IdButaca { get; set; }

    public int IdSala { get; set; }

    public string Fila { get; set; } = null!;

    public int Numero { get; set; }

    public virtual ICollection<EstadoButacaFuncion> EstadoButacaFuncions { get; set; } = new List<EstadoButacaFuncion>();

    public virtual Sala IdSalaNavigation { get; set; } = null!;
}
