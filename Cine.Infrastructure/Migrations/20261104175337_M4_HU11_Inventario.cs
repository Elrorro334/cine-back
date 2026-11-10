using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M4_HU11_Inventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "producto",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "factor_equivalencia",
                table: "producto",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "cantidad",
                table: "entrada_mercancia",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_stock_agregada",
                table: "entrada_mercancia",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "costo_unitario",
                table: "entrada_mercancia",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "id_factura",
                table: "entrada_mercancia",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "proveedor",
                columns: table => new
                {
                    id_proveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    rfc = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    telefono = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    contacto = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__proveedo__8D3C3E3B", x => x.id_proveedor);
                });

            migrationBuilder.CreateTable(
                name: "facturas_proveedor",
                columns: table => new
                {
                    id_factura = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_proveedor = table.Column<int>(type: "int", nullable: false),
                    folio = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    total_pagado = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    id_vendedor = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__facturas__8A7E01E2", x => x.id_factura);
                    table.ForeignKey(
                        name: "FK_factura_proveedor",
                        column: x => x.id_proveedor,
                        principalTable: "proveedor",
                        principalColumn: "id_proveedor");
                    table.ForeignKey(
                        name: "FK_factura_vendedor",
                        column: x => x.id_vendedor,
                        principalTable: "vendedor",
                        principalColumn: "id_vendedor",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entrada_mercancia_id_factura",
                table: "entrada_mercancia",
                column: "id_factura");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_proveedor_id_vendedor",
                table: "facturas_proveedor",
                column: "id_vendedor");

            migrationBuilder.CreateIndex(
                name: "UQ_facturas_proveedor_folio",
                table: "facturas_proveedor",
                columns: new[] { "id_proveedor", "folio" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_entrada_factura",
                table: "entrada_mercancia",
                column: "id_factura",
                principalTable: "facturas_proveedor",
                principalColumn: "id_factura",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entrada_factura",
                table: "entrada_mercancia");

            migrationBuilder.DropTable(
                name: "facturas_proveedor");

            migrationBuilder.DropTable(
                name: "proveedor");

            migrationBuilder.DropIndex(
                name: "IX_entrada_mercancia_id_factura",
                table: "entrada_mercancia");

            migrationBuilder.DropColumn(
                name: "activo",
                table: "producto");

            migrationBuilder.DropColumn(
                name: "factor_equivalencia",
                table: "producto");

            migrationBuilder.DropColumn(
                name: "cantidad",
                table: "entrada_mercancia");

            migrationBuilder.DropColumn(
                name: "cantidad_stock_agregada",
                table: "entrada_mercancia");

            migrationBuilder.DropColumn(
                name: "costo_unitario",
                table: "entrada_mercancia");

            migrationBuilder.DropColumn(
                name: "id_factura",
                table: "entrada_mercancia");
        }
    }
}
