using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Infrastructure.Persistence;

/// <summary>
/// Datos base requeridos por la operación de taquilla.
/// La siembra es idempotente: solo agrega butacas faltantes por sala.
/// </summary>
public static class DbSeeder
{
    // Distribución física del mapa de butacas (Mockup 11 / HU-06).
    private static readonly Dictionary<int, (int Filas, int ButacasPorFila)> Distribuciones = new()
    {
        [1] = (6, 8), // Sala 1: A1..F8 = 48 butacas
        [2] = (5, 8), // Sala 2: A1..E8 = 40 butacas
    };

    public static async Task SeedButacasAsync(CineDbContext db)
    {
        var salas = (await db.Salas.ToListAsync())
            .Where(s => Distribuciones.ContainsKey(s.IdSala))
            .ToList();

        foreach (var sala in salas)
        {
            var (filas, butacasPorFila) = Distribuciones[sala.IdSala];

            var existentes = await db.Butacas
                .Where(b => b.IdSala == sala.IdSala)
                .Select(b => new { b.Fila, b.Numero })
                .ToListAsync();

            var codigosExistentes = existentes
                .Select(b => $"{b.Fila}{b.Numero}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (var fila = 'A'; fila < 'A' + filas; fila++)
            {
                for (var numero = 1; numero <= butacasPorFila; numero++)
                {
                    var codigo = $"{fila}{numero}";
                    if (codigosExistentes.Contains(codigo)) continue;

                    db.Butacas.Add(new Butaca
                    {
                        IdSala = sala.IdSala,
                        Fila = fila.ToString(),
                        Numero = numero,
                    });
                }
            }

            var capacidadEsperada = filas * butacasPorFila;
            if (sala.CapacidadTotal != capacidadEsperada)
            {
                sala.CapacidadTotal = capacidadEsperada;
            }
        }

        await db.SaveChangesAsync();
    }
}
