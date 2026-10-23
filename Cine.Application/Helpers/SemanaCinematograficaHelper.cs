namespace Cine.Application.Helpers;

/// <summary>
/// Provee utilidades para el cálculo y manejo de la Semana Cinematográfica.
/// Regla de negocio HU-10: La semana cinematográfica estándar inicia en Jueves y concluye en Miércoles.
/// </summary>
public static class SemanaCinematograficaHelper
{
    public static (DateTime Inicio, DateTime Fin, string Descripcion) CalcularSemana(DateTime? fechaReferencia = null, int offsetSemanas = 0)
    {
        var referencia = fechaReferencia?.Date ?? DateTime.Today;

        // Calcular la distancia en días con respecto al jueves anterior o actual
        // DayOfWeek: Sunday = 0, Monday = 1, Tuesday = 2, Wednesday = 3, Thursday = 4, Friday = 5, Saturday = 6
        int diff = ((int)referencia.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;

        var inicioJueves = referencia.AddDays(-diff).AddDays(7 * offsetSemanas);
        var finMiercoles = inicioJueves.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        string descripcion = $"Semana Cinematográfica del {inicioJueves:dd/MM/yyyy} al {finMiercoles:dd/MM/yyyy}";

        return (inicioJueves, finMiercoles, descripcion);
    }
}
