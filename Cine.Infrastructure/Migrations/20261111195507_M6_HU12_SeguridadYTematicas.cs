using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Infrastructure.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class M6_HU12_SeguridadYTematicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IpDefecto = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tematicas_Sitio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreTematica = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ClaveIdentificadora = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EsActiva = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaActivacion = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tematicas_Sitio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turnos_Laborales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreTurno = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos_Laborales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configuracion_Seguridad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    IpAutorizada = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false),
                    HorarioInicioCajeros = table.Column<TimeSpan>(type: "time", nullable: false),
                    HorarioFinCajeros = table.Column<TimeSpan>(type: "time", nullable: false),
                    AutorizarExcepcionHorasExtras = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime", nullable: false),
                    UsuarioModificoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion_Seguridad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionSeguridad_Sucursal",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Plantillas_CSS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TematicaId = table.Column<int>(type: "int", nullable: false),
                    RutaArchivoCss = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    PaletaColoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipografia = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantillas_CSS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasCss_Tematica",
                        column: x => x.TematicaId,
                        principalTable: "Tematicas_Sitio",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Configuracion_Seguridad_SucursalId",
                table: "Configuracion_Seguridad",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantillas_CSS_TematicaId",
                table: "Plantillas_CSS",
                column: "TematicaId");

            migrationBuilder.CreateIndex(
                name: "UQ_Tematicas_Sitio_NombreTematica",
                table: "Tematicas_Sitio",
                column: "NombreTematica",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracion_Seguridad");

            migrationBuilder.DropTable(
                name: "Plantillas_CSS");

            migrationBuilder.DropTable(
                name: "Turnos_Laborales");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "Tematicas_Sitio");
        }
    }
}
