using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities
{

    public partial class EstadoButacaFuncion
    {
        public int IdEstado { get; set; }

        public int IdButaca { get; set; }

        public int IdFuncion { get; set; }

        public bool Estato { get; set; }

        public virtual Butaca IdButacaNavigation { get; set; } = null!;
    }
}
