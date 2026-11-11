namespace Cine.Application.Dtos;

public class SolicitarCodigoPreordenDto
{
    public string Telefono { get; set; } = string.Empty;
}

public class SolicitarCodigoPreordenResponseDto
{
    public bool Enviado { get; set; }
    public string TelefonoEnmascarado { get; set; } = string.Empty;
    public int ExpiraEnMinutos { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VerificarCodigoPreordenDto
{
    public string Telefono { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}
