using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Infrastructure.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class M5_HU08_HU09_PreordenQrYCobroTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "canje_preorden",
                columns: table => new
                {
                    id_canje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_preorden = table.Column<int>(type: "int", nullable: false),
                    id_venta = table.Column<int>(type: "int", nullable: false),
                    id_vendedor = table.Column<int>(type: "int", nullable: false),
                    codigo_qr = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    fecha_canje = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    monto_canjeado = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_canje_preorden", x => x.id_canje);
                    table.ForeignKey(
                        name: "FK_canje_preorden_preorden",
                        column: x => x.id_preorden,
                        principalTable: "preorden",
                        principalColumn: "id_preorden");
                    table.ForeignKey(
                        name: "FK_canje_preorden_venta",
                        column: x => x.id_venta,
                        principalTable: "venta",
                        principalColumn: "id_venta");
                    table.ForeignKey(
                        name: "FK_canje_preorden_vendedor",
                        column: x => x.id_vendedor,
                        principalTable: "vendedor",
                        principalColumn: "id_vendedor");
                });

            migrationBuilder.CreateTable(
                name: "pago_venta",
                columns: table => new
                {
                    id_pago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_venta = table.Column<int>(type: "int", nullable: false),
                    metodo_pago = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    monto_total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    monto_recibido = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    cambio = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    folio_autorizacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    codigo_barras_ticket = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    rfc_emisor = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "CTX-260414-9X0"),
                    fecha_pago = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pago_venta", x => x.id_pago);
                    table.ForeignKey(
                        name: "FK_pago_venta_venta",
                        column: x => x.id_venta,
                        principalTable: "venta",
                        principalColumn: "id_venta");
                });

            migrationBuilder.CreateIndex(
                name: "IX_canje_preorden_id_preorden",
                table: "canje_preorden",
                column: "id_preorden");

            migrationBuilder.CreateIndex(
                name: "IX_canje_preorden_id_venta",
                table: "canje_preorden",
                column: "id_venta");

            migrationBuilder.CreateIndex(
                name: "IX_canje_preorden_id_vendedor",
                table: "canje_preorden",
                column: "id_vendedor");

            migrationBuilder.CreateIndex(
                name: "IX_pago_venta_id_venta",
                table: "pago_venta",
                column: "id_venta");

            // Stored Procedure HU-08: Validar Pre-Orden QR (vigencia <= 12 días y no canjeada previamente)
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[sp_ValidarPreordenQr]
    @CodigoQr VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.id_preorden AS IdPreorden,
        p.codigo_qr AS CodigoQr,
        p.id_funcion AS IdFuncion,
        p.id_cliente AS IdCliente,
        p.fecha_creacion AS FechaCreacion,
        p.vigencia AS Vigencia,
        p.estado_activo AS EstadoActivo,
        DATEDIFF(DAY, CAST(p.fecha_creacion AS DATE), CAST(SYSDATETIME() AS DATE)) AS DiasTranscurridos,
        CASE
            WHEN EXISTS (SELECT 1 FROM [dbo].[canje_preorden] cp WHERE cp.id_preorden = p.id_preorden)
                 OR p.estado_activo = 0 THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS YaCanjeada,
        CASE
            WHEN DATEDIFF(DAY, CAST(p.fecha_creacion AS DATE), CAST(SYSDATETIME() AS DATE)) > 12 THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS Expirada
    FROM [dbo].[preorden] p
    WHERE p.codigo_qr = @CodigoQr;
END
");

            // Stored Procedure HU-09: Registrar Pago en Efectivo / Mercado Pago, Cambio, Código de Barras y Canje de Pre-Orden
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[sp_RegistrarPagoYCanjePos]
    @IdVenta INT,
    @IdVendedor INT,
    @MetodoPago VARCHAR(50),
    @MontoTotal DECIMAL(12,2),
    @MontoRecibido DECIMAL(12,2),
    @FolioAutorizacion VARCHAR(50) = NULL,
    @CodigoBarrasTicket VARCHAR(30),
    @CodigoPreorden VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @MetodoPago = 'Efectivo' AND @MontoRecibido < @MontoTotal
    BEGIN
        THROW 50010, 'El efectivo recibido es menor al monto total a cobrar.', 1;
    END

    DECLARE @CambioCalculado DECIMAL(12,2) = CASE
        WHEN @MontoRecibido > @MontoTotal THEN (@MontoRecibido - @MontoTotal)
        ELSE 0.00
    END;

    INSERT INTO [dbo].[pago_venta]
        (id_venta, metodo_pago, monto_total, monto_recibido, cambio, folio_autorizacion, codigo_barras_ticket, rfc_emisor, fecha_pago)
    VALUES
        (@IdVenta, @MetodoPago, @MontoTotal, @MontoRecibido, @CambioCalculado, @FolioAutorizacion, @CodigoBarrasTicket, 'CTX-260414-9X0', SYSDATETIME());

    IF @CodigoPreorden IS NOT NULL AND LTRIM(RTRIM(@CodigoPreorden)) <> ''
    BEGIN
        DECLARE @IdPreorden INT = NULL;
        SELECT TOP 1 @IdPreorden = id_preorden
        FROM [dbo].[preorden]
        WHERE codigo_qr = @CodigoPreorden;

        IF @IdPreorden IS NOT NULL
        BEGIN
            UPDATE [dbo].[preorden]
            SET estado_activo = 0
            WHERE id_preorden = @IdPreorden;

            INSERT INTO [dbo].[canje_preorden]
                (id_preorden, id_venta, id_vendedor, codigo_qr, fecha_canje, monto_canjeado)
            VALUES
                (@IdPreorden, @IdVenta, @IdVendedor, @CodigoPreorden, SYSDATETIME(), @MontoTotal);
        END
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_RegistrarPagoYCanjePos];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_ValidarPreordenQr];");

            migrationBuilder.DropTable(
                name: "pago_venta");

            migrationBuilder.DropTable(
                name: "canje_preorden");
        }
    }
}
