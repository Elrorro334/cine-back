namespace cine_back.Dtos;

public class FuncionHorarioDto
{
    public int IdFuncion { get; set; }

    // Lo mandamos como string para que el frontend (PWA) lo imprima directo en el botón
    // sin tener que parsear fechas complejas con JavaScript.
    public string Hora { get; set; } = string.Empty;

    public string NombreSala { get; set; } = string.Empty;
}