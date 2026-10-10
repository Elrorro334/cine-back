using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Preorden
{
    public int IdPreorden { get; set; }

    public string? CodigoQr { get; set; }

    public int IdFuncion { get; set; }

    public int IdCliente { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int Vigencia { get; set; }

    public bool EstadoActivo { get; set; }

    public virtual ICollection<DetallePreordenBoleto> DetallePreordenBoletos { get; set; } = new List<DetallePreordenBoleto>();

    public virtual ICollection<DetallePreordenDulcerium> DetallePreordenDulceria { get; set; } = new List<DetallePreordenDulcerium>();

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual Funcion IdFuncionNavigation { get; set; } = null!;
}
