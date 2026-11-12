using System;

namespace Cine.Application.Exceptions;

public class SubredDiferenteException : InvalidOperationException
{
    public string IpClienteDetectada { get; }
    public string IpSolicitada { get; }

    public SubredDiferenteException(string ipClienteDetectada, string ipSolicitada, string message)
        : base(message)
    {
        IpClienteDetectada = ipClienteDetectada;
        IpSolicitada = ipSolicitada;
    }
}
