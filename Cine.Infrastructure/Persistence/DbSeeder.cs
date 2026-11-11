using System.Diagnostics.CodeAnalysis;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Infrastructure.Persistence;

/// <summary>
/// Datos base requeridos por la operación de taquilla y dulcería POS (HU-06 / HU-07).
/// La siembra es idempotente: solo agrega registros faltantes sin sobrescribir ventas previas.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DbSeeder
{
    // Distribución física del mapa de butacas (Mockup 11 / HU-06).
    private static readonly Dictionary<int, (int Filas, int ButacasPorFila)> Distribuciones = new()
    {
        [1] = (6, 8), // Sala 1: A1..F8 = 48 butacas
        [2] = (5, 8), // Sala 2: A1..E8 = 40 butacas
    };

    private static readonly (string Nombre, string Descripcion)[] CategoriasBase =
    {
        ("Taquilla", "Boletos de acceso general a funciones"),
        ("Snacks", "Botanas y alimentos de dulcería"),
        ("Bebidas", "Refrescos y bebidas frías"),
        ("Pizzas", "Inventario fraccionado de pizzas y rebanadas")
    };

    private static readonly (string Sku, string Nombre, string Categoria, decimal Precio, decimal Costo, int Stock, int StockMin)[] ProductosBase =
    {
        ("BOL-GEN", "Boleto General", "Taquilla", 50.00m, 0.00m, 999, 10),
        ("7501001", "Palomitas Grandes", "Snacks", 50.00m, 18.00m, 40, 10),
        ("7501002", "Refresco 600ml", "Bebidas", 35.00m, 14.00m, 50, 12),
        ("7501003", "Nachos con Queso", "Snacks", 65.00m, 25.00m, 30, 8),
        ("7501004", "Chips / Frituras", "Snacks", 25.00m, 10.00m, 35, 10),
        ("7501005", "Icee / Granizado", "Bebidas", 45.00m, 15.00m, 25, 8),
        ("7501006", "Rebanada Individual", "Pizzas", 35.00m, 12.00m, 56, 8),
        ("7501007", "Pizza Completa (8 Rebanadas)", "Pizzas", 180.00m, 85.00m, 7, 1)
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

    public static async Task SeedDulceriaAsync(CineDbContext db)
    {
        var categoriasExistentes = await db.Categoria.ToListAsync();
        foreach (var (nombre, descripcion) in CategoriasBase)
        {
            if (categoriasExistentes.Any(c => string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var nuevaCat = new Categorium
            {
                Nombre = nombre,
                Descripcion = descripcion
            };
            db.Categoria.Add(nuevaCat);
            categoriasExistentes.Add(nuevaCat);
        }

        await db.SaveChangesAsync();

        var mapaCategorias = (await db.Categoria.ToListAsync())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
            .GroupBy(c => c.Nombre!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().IdCategoria, StringComparer.OrdinalIgnoreCase);

        var productosExistentes = await db.Productos.ToListAsync();
        foreach (var item in ProductosBase)
        {
            if (productosExistentes.Any(p => string.Equals(p.CodigoBarras, item.Sku, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!mapaCategorias.TryGetValue(item.Categoria, out var idCategoria))
            {
                idCategoria = mapaCategorias.Values.FirstOrDefault(1);
            }

            db.Productos.Add(new Producto
            {
                CodigoBarras = item.Sku,
                Nombre = item.Nombre,
                IdCategoria = idCategoria,
                PrecioVenta = item.Precio,
                Costo = item.Costo,
                StockActual = item.Stock,
                StockMinimo = item.StockMin
            });
        }

        await db.SaveChangesAsync();

        var existeFraccionado = await db.InventarioStockFraccionados.AnyAsync();
        if (!existeFraccionado)
        {
            var prodRebanada = await db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501006");
            var prodPizza = await db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501007");

            if (prodRebanada != null && prodPizza != null)
            {
                var rebanadasActuales = prodRebanada.StockActual > 0 ? prodRebanada.StockActual : 56;
                db.InventarioStockFraccionados.Add(new InventarioStockFraccionado
                {
                    IdProductoMaestro = prodPizza.IdProducto,
                    IdProductoFraccion = prodRebanada.IdProducto,
                    FactorFraccion = 8,
                    StockFraccionesActual = rebanadasActuales,
                    StockFraccionesMinimo = 16,
                    UnidadMedidaFraccion = "rebanadas",
                    UnidadMedidaMaestra = "pizzas",
                    UltimaActualizacion = DateTime.Now
                });
                await db.SaveChangesAsync();
            }
        }
    }

    public static async Task EnsureSprint3TablesAsync(CineDbContext db)
    {
        if (!db.Database.IsRelational()) return;

        await db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('dbo.producto', 'costo') IS NULL
    ALTER TABLE dbo.producto ADD costo DECIMAL(12,2) NOT NULL DEFAULT(0);
IF COL_LENGTH('dbo.producto', 'stock_minimo') IS NULL
    ALTER TABLE dbo.producto ADD stock_minimo INT NOT NULL DEFAULT(5);
IF COL_LENGTH('dbo.producto', 'factor_equivalencia') IS NULL
    ALTER TABLE dbo.producto ADD factor_equivalencia INT NOT NULL DEFAULT(1);
IF COL_LENGTH('dbo.producto', 'activo') IS NULL
    ALTER TABLE dbo.producto ADD activo BIT NOT NULL DEFAULT(1);

IF OBJECT_ID('dbo.inventario_stock_fraccionado', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.inventario_stock_fraccionado (
        id_stock_fraccionado INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_inventario_stock_fraccionado PRIMARY KEY,
        id_producto_maestro INT NOT NULL,
        id_producto_fraccion INT NOT NULL,
        factor_fraccion INT NOT NULL DEFAULT(8),
        stock_fracciones_actual INT NOT NULL DEFAULT(56),
        stock_fracciones_minimo INT NOT NULL DEFAULT(16),
        unidad_medida_fraccion VARCHAR(50) NOT NULL DEFAULT('rebanadas'),
        unidad_medida_maestra VARCHAR(50) NOT NULL DEFAULT('pizzas'),
        ultima_actualizacion DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_stock_fraccionado_producto_maestro FOREIGN KEY (id_producto_maestro) REFERENCES dbo.producto(id_producto),
        CONSTRAINT FK_stock_fraccionado_producto_fraccion FOREIGN KEY (id_producto_fraccion) REFERENCES dbo.producto(id_producto)
    );
END;

IF OBJECT_ID('dbo.canje_preorden', 'U') IS NOT NULL AND COL_LENGTH('dbo.canje_preorden', 'codigo_qr') IS NULL
    DROP TABLE dbo.canje_preorden;

IF OBJECT_ID('dbo.canje_preorden', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.canje_preorden (
        id_canje INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_canje_preorden PRIMARY KEY,
        id_preorden INT NOT NULL,
        id_venta INT NOT NULL,
        id_vendedor INT NOT NULL DEFAULT(1),
        codigo_qr VARCHAR(50) NOT NULL DEFAULT(''),
        fecha_canje DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        monto_canjeado DECIMAL(12,2) NOT NULL DEFAULT(0),
        CONSTRAINT FK_canje_preorden_preorden FOREIGN KEY (id_preorden) REFERENCES dbo.preorden(id_preorden),
        CONSTRAINT FK_canje_preorden_venta FOREIGN KEY (id_venta) REFERENCES dbo.venta(id_venta)
    );
    CREATE INDEX IX_canje_preorden_id_preorden ON dbo.canje_preorden(id_preorden);
    CREATE INDEX IX_canje_preorden_id_venta ON dbo.canje_preorden(id_venta);
END;

IF OBJECT_ID('dbo.pago_venta', 'U') IS NOT NULL AND COL_LENGTH('dbo.pago_venta', 'monto_total') IS NULL
    DROP TABLE dbo.pago_venta;

IF OBJECT_ID('dbo.pago_venta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.pago_venta (
        id_pago INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_pago_venta PRIMARY KEY,
        id_venta INT NOT NULL,
        metodo_pago VARCHAR(50) NOT NULL,
        monto_total DECIMAL(12,2) NOT NULL,
        monto_recibido DECIMAL(12,2) NOT NULL,
        cambio DECIMAL(12,2) NOT NULL,
        folio_autorizacion VARCHAR(50) NULL,
        codigo_barras_ticket VARCHAR(30) NOT NULL DEFAULT(''),
        rfc_emisor VARCHAR(20) NOT NULL DEFAULT('CTX-260414-9X0'),
        fecha_pago DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_pago_venta_venta FOREIGN KEY (id_venta) REFERENCES dbo.venta(id_venta)
    );
    CREATE INDEX IX_pago_venta_id_venta ON dbo.pago_venta(id_venta);
END;

IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261111064500_M5_HU08_HU09_PreordenQrYCobroTicket')
    BEGIN
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20261111064500_M5_HU08_HU09_PreordenQrYCobroTicket', '10.0.3');
    END
END;");
    }

    public static async Task SeedPreordenesDemoAsync(CineDbContext db)
    {
        var hoy = DateTime.Today;
        var sala = await db.Salas.FirstOrDefaultAsync();
        if (sala == null)
        {
            sala = new Sala { NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "Tradicional" };
            db.Salas.Add(sala);
            await db.SaveChangesAsync();
        }

        if (!await db.Peliculas.AnyAsync())
        {
            db.Peliculas.AddRange(
                new Pelicula { Titulo = "Terrifier 3: Payaso Siniestro", Clasificacion = "C", Duracion = 125, Director = "Damien Leone", Sinopsis = "El payaso Art desata el caos en Nochebuena.", PosterImage = "/terrifier3.jpg" },
                new Pelicula { Titulo = "Spider-Man: Un Nuevo Día", Clasificacion = "B", Duracion = 130, Director = "Destin Daniel Cretton", Sinopsis = "Peter Parker enfrenta un nuevo comienzo en Nueva York.", PosterImage = "/spiderman.jpg" },
                new Pelicula { Titulo = "Avatar: Fuego y Ceniza", Clasificacion = "B", Duracion = 180, Director = "James Cameron", Sinopsis = "Jake Sully y Neytiri conocen al clan de la ceniza.", PosterImage = "/avatar3.jpg" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Funcions.AnyAsync(f => f.FechaFuncion.Date == hoy))
        {
            var peliculas = await db.Peliculas.Take(4).ToListAsync();
            var horarios = new[] { new TimeSpan(12, 30, 0), new TimeSpan(15, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(20, 30, 0) };
            foreach (var peli in peliculas)
            {
                foreach (var h in horarios)
                {
                    db.Funcions.Add(new Funcion
                    {
                        IdPelicula = peli.IdPelicula,
                        IdSala = sala.IdSala,
                        FechaFuncion = hoy,
                        HoraInicio = hoy.Add(h),
                        Precio = 50.00m
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        var funcionHoy = await db.Funcions.FirstOrDefaultAsync(f => f.FechaFuncion.Date == hoy)
                         ?? await db.Funcions.FirstOrDefaultAsync();
        if (funcionHoy == null) return;

        var clienteDemo = await db.Clientes.FirstOrDefaultAsync(c => c.NumeroTelefono == "5512345678");
        if (clienteDemo == null)
        {
            clienteDemo = new Cliente
            {
                Nombre = "Carlos Ruiz",
                NumeroTelefono = "5512345678",
                Correo = "carlos.ruiz@xilotzin.local"
            };
            db.Clientes.Add(clienteDemo);
            await db.SaveChangesAsync();
        }

        var prodNachos = await db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501003")
                         ?? await db.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501001");

        // 1. Pre-orden ACTIVA y VIGENTE (PRE-XIL-84920) para demostración de HU-08 y HU-09 ($115.00 = 1 Boleto $50 + 1 Nachos $65)
        if (!await db.Preordens.AnyAsync(p => p.CodigoQr == "PRE-XIL-84920"))
        {
            var preActiva = new Preorden
            {
                CodigoQr = "PRE-XIL-84920",
                IdCliente = clienteDemo.IdCliente,
                IdFuncion = funcionHoy.IdFuncion,
                FechaCreacion = DateTime.Now,
                Vigencia = 12,
                EstadoActivo = true
            };
            db.Preordens.Add(preActiva);
            await db.SaveChangesAsync();

            db.DetallePreordenBoletos.Add(new DetallePreordenBoleto
            {
                IdPreorden = preActiva.IdPreorden,
                Caantidad = 1,
                Subtotal = 50.00m
            });

            if (prodNachos != null)
            {
                db.DetallePreordenDulceria.Add(new DetallePreordenDulcerium
                {
                    IdPreorden = preActiva.IdPreorden,
                    IdProducto = prodNachos.IdProducto,
                    Cantidad = 1
                });
            }
            await db.SaveChangesAsync();
        }

        // 2. Pre-orden EXPIRADA (> 12 días: PRE-XIL-EXP15) para demostrar rechazo por vigencia en HU-08
        if (!await db.Preordens.AnyAsync(p => p.CodigoQr == "PRE-XIL-EXP15"))
        {
            var preExpirada = new Preorden
            {
                CodigoQr = "PRE-XIL-EXP15",
                IdCliente = clienteDemo.IdCliente,
                IdFuncion = funcionHoy.IdFuncion,
                FechaCreacion = DateTime.Now.AddDays(-15),
                Vigencia = 12,
                EstadoActivo = true
            };
            db.Preordens.Add(preExpirada);
            await db.SaveChangesAsync();

            db.DetallePreordenBoletos.Add(new DetallePreordenBoleto
            {
                IdPreorden = preExpirada.IdPreorden,
                Caantidad = 2,
                Subtotal = 100.00m
            });
            await db.SaveChangesAsync();
        }

        // 3. Pre-orden YA CANJEADA (PRE-XIL-USADA) para demostrar rechazo por estatus Canjeado en HU-08
        if (!await db.Preordens.AnyAsync(p => p.CodigoQr == "PRE-XIL-USADA"))
        {
            var preUsada = new Preorden
            {
                CodigoQr = "PRE-XIL-USADA",
                IdCliente = clienteDemo.IdCliente,
                IdFuncion = funcionHoy.IdFuncion,
                FechaCreacion = DateTime.Now.AddDays(-2),
                Vigencia = 12,
                EstadoActivo = false
            };
            db.Preordens.Add(preUsada);
            await db.SaveChangesAsync();

            db.DetallePreordenBoletos.Add(new DetallePreordenBoleto
            {
                IdPreorden = preUsada.IdPreorden,
                Caantidad = 1,
                Subtotal = 50.00m
            });
            await db.SaveChangesAsync();
        }
    }
}
