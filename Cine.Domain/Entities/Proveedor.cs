using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities
{
    public partial class Proveedor
    {
        public int IdProveedor { get; set; }

        public string Nombre { get; set; } = null!;

        public string? Rfc { get; set; }

        public string? Telefono { get; set; }

        public string? Contacto { get; set; }

        public bool Activo { get; set; } = true;

        public virtual ICollection<FacturaProveedor> Facturas { get; set; } = new List<FacturaProveedor>();
    }
}
