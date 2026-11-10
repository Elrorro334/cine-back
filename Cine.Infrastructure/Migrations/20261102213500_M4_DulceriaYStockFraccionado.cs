using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M4_DulceriaYStockFraccionado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventario_stock_fraccionado",
                columns: table => new
                {
                    id_stock_fraccionado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_producto_maestro = table.Column<int>(type: "int", nullable: false),
                    id_producto_fraccion = table.Column<int>(type: "int", nullable: false),
                    factor_fraccion = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    stock_fracciones_actual = table.Column<int>(type: "int", nullable: false, defaultValue: 56),
                    stock_fracciones_minimo = table.Column<int>(type: "int", nullable: false, defaultValue: 16),
                    unidad_medida_fraccion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    unidad_medida_maestra = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ultima_actualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventario_stock_fraccionado", x => x.id_stock_fraccionado);
                    table.ForeignKey(
                        name: "FK_stock_fraccionado_producto_fraccion",
                        column: x => x.id_producto_fraccion,
                        principalTable: "producto",
                        principalColumn: "id_producto");
                    table.ForeignKey(
                        name: "FK_stock_fraccionado_producto_maestro",
                        column: x => x.id_producto_maestro,
                        principalTable: "producto",
                        principalColumn: "id_producto");
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventario_stock_fraccionado_id_producto_fraccion",
                table: "inventario_stock_fraccionado",
                column: "id_producto_fraccion");

            migrationBuilder.CreateIndex(
                name: "IX_inventario_stock_fraccionado_id_producto_maestro",
                table: "inventario_stock_fraccionado",
                column: "id_producto_maestro");

            migrationBuilder.CreateIndex(
                name: "IX_producto_codigo_barras",
                table: "producto",
                column: "codigo_barras");

            // Stored Procedure 1: Consultar catálogo de dulcería POS e inventario fraccionado
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[sp_ObtenerCatalogoDulceriaPos]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.id_producto AS IdProducto,
        p.codigo_barras AS CodigoBarras,
        p.nombre AS Nombre,
        ISNULL(c.nombre, 'Snacks') AS Categoria,
        p.precio_venta AS PrecioVenta,
        p.costo AS Costo,
        CASE
            WHEN sf_frac.id_stock_fraccionado IS NOT NULL THEN sf_frac.stock_fracciones_actual
            WHEN sf_maes.id_stock_fraccionado IS NOT NULL THEN (sf_maes.stock_fracciones_actual / NULLIF(sf_maes.factor_fraccion, 0))
            ELSE p.stock_actual
        END AS StockActual,
        p.stock_minimo AS StockMinimo,
        CASE
            WHEN sf_frac.id_stock_fraccionado IS NOT NULL OR sf_maes.id_stock_fraccionado IS NOT NULL THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS EsFraccionado,
        CASE
            WHEN sf_maes.id_stock_fraccionado IS NOT NULL THEN sf_maes.factor_fraccion
            ELSE 1
        END AS FactorFraccion
    FROM [dbo].[producto] p
    LEFT JOIN [dbo].[categoria] c ON p.id_categoria = c.id_categoria
    LEFT JOIN [dbo].[inventario_stock_fraccionado] sf_frac ON p.id_producto = sf_frac.id_producto_fraccion
    LEFT JOIN [dbo].[inventario_stock_fraccionado] sf_maes ON p.id_producto = sf_maes.id_producto_maestro
    WHERE ISNULL(c.nombre, '') <> 'Taquilla'
    ORDER BY p.codigo_barras ASC;
END
");

            // Stored Procedure 2: Búsqueda rápida por lector de código de barras (SKU)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[sp_BuscarProductoPorSku]
    @CodigoBarras NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.id_producto AS IdProducto,
        p.codigo_barras AS CodigoBarras,
        p.nombre AS Nombre,
        ISNULL(c.nombre, 'Snacks') AS Categoria,
        p.precio_venta AS PrecioVenta,
        CASE
            WHEN sf_frac.id_stock_fraccionado IS NOT NULL THEN sf_frac.stock_fracciones_actual
            WHEN sf_maes.id_stock_fraccionado IS NOT NULL THEN (sf_maes.stock_fracciones_actual / NULLIF(sf_maes.factor_fraccion, 0))
            ELSE p.stock_actual
        END AS StockActual,
        CASE
            WHEN sf_frac.id_stock_fraccionado IS NOT NULL OR sf_maes.id_stock_fraccionado IS NOT NULL THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS EsFraccionado,
        CASE
            WHEN sf_maes.id_stock_fraccionado IS NOT NULL THEN sf_maes.factor_fraccion
            ELSE 1
        END AS FactorFraccion
    FROM [dbo].[producto] p
    LEFT JOIN [dbo].[categoria] c ON p.id_categoria = c.id_categoria
    LEFT JOIN [dbo].[inventario_stock_fraccionado] sf_frac ON p.id_producto = sf_frac.id_producto_fraccion
    LEFT JOIN [dbo].[inventario_stock_fraccionado] sf_maes ON p.id_producto = sf_maes.id_producto_maestro
    WHERE p.codigo_barras = @CodigoBarras
      AND ISNULL(c.nombre, '') <> 'Taquilla';
END
");

            // Stored Procedure 3: Descuento atómico y proporcional de stock normal y fraccionado (1 pizza = 8 rebanadas)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[sp_DescontarStockDulceria]
    @IdProducto INT,
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Cantidad <= 0
    BEGIN
        THROW 50001, 'La cantidad a descontar debe ser mayor a cero.', 1;
    END

    DECLARE @IdStockFraccionado INT = NULL;
    DECLARE @IdMaestro INT = NULL;
    DECLARE @IdFraccion INT = NULL;
    DECLARE @Factor INT = 8;
    DECLARE @StockFracciones INT = 0;

    SELECT TOP 1
        @IdStockFraccionado = id_stock_fraccionado,
        @IdMaestro = id_producto_maestro,
        @IdFraccion = id_producto_fraccion,
        @Factor = CASE WHEN factor_fraccion > 0 THEN factor_fraccion ELSE 8 END,
        @StockFracciones = stock_fracciones_actual
    FROM [dbo].[inventario_stock_fraccionado]
    WHERE id_producto_fraccion = @IdProducto OR id_producto_maestro = @IdProducto;

    IF @IdStockFraccionado IS NOT NULL
    BEGIN
        DECLARE @RebanadasDescontar INT = CASE
            WHEN @IdProducto = @IdMaestro THEN (@Cantidad * @Factor)
            ELSE @Cantidad
        END;

        IF @StockFracciones <= 0 OR @RebanadasDescontar > @StockFracciones
        BEGIN
            THROW 50002, 'Stock fraccionado insuficiente para procesar la venta.', 1;
        END

        DECLARE @NuevoStockRebanadas INT = @StockFracciones - @RebanadasDescontar;
        DECLARE @NuevoStockMaestro INT = @NuevoStockRebanadas / @Factor;

        UPDATE [dbo].[inventario_stock_fraccionado]
        SET stock_fracciones_actual = @NuevoStockRebanadas,
            ultima_actualizacion = SYSDATETIME()
        WHERE id_stock_fraccionado = @IdStockFraccionado;

        UPDATE [dbo].[producto]
        SET stock_actual = @NuevoStockRebanadas
        WHERE id_producto = @IdFraccion;

        UPDATE [dbo].[producto]
        SET stock_actual = @NuevoStockMaestro
        WHERE id_producto = @IdMaestro;

        RETURN;
    END

    DECLARE @StockActualNormal INT = 0;
    SELECT @StockActualNormal = stock_actual
    FROM [dbo].[producto]
    WHERE id_producto = @IdProducto;

    IF @StockActualNormal <= 0 OR @Cantidad > @StockActualNormal
    BEGIN
        THROW 50003, 'El producto no cuenta con inventario suficiente para la venta.', 1;
    END

    UPDATE [dbo].[producto]
    SET stock_actual = stock_actual - @Cantidad
    WHERE id_producto = @IdProducto;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_DescontarStockDulceria];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_BuscarProductoPorSku];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_ObtenerCatalogoDulceriaPos];");

            migrationBuilder.DropIndex(
                name: "IX_producto_codigo_barras",
                table: "producto");

            migrationBuilder.DropTable(
                name: "inventario_stock_fraccionado");
        }
    }
}
