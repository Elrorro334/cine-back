using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities
{
    public partial class RedesAutorizada
    {
        public int IdRed { get; set; }

        public string DireccionIp { get; set; } = null!;
    }
}
