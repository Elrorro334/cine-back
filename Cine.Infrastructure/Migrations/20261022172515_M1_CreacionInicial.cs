using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1_CreacionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    descripcion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__categori__CD54BC5AB14327EA", x => x.id_categoria);
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                columns: table => new
                {
                    id_cliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    numero_telefono = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    correo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__cliente__677F38F5A756C86F", x => x.id_cliente);
                });

            migrationBuilder.CreateTable(
                name: "pelicula",
                columns: table => new
                {
                    id_pelicula = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    titulo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    sinopsis = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    duracion = table.Column<int>(type: "int", nullable: false),
                    clasificacion = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    director = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    poster_image = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pelicula__B5017F4D9F56E1A2", x => x.id_pelicula);
                });

            migrationBuilder.CreateTable(
                name: "redes_autorizadas",
                columns: table => new
                {
                    id_red = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    direccion_ip = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__redes_au__6ABE6F0BF6D6E259", x => x.id_red);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_rol = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__rol__6ABCB5E0179135A5", x => x.id_rol);
                });

            migrationBuilder.CreateTable(
                name: "sala",
                columns: table => new
                {
                    id_sala = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre_sala = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    capacidad_total = table.Column<int>(type: "int", nullable: false),
                    tipo_pantalla = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sala__D18B015B01491D56", x => x.id_sala);
                });

            migrationBuilder.CreateTable(
                name: "producto",
                columns: table => new
                {
                    id_producto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_barras = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    id_categoria = table.Column<int>(type: "int", nullable: false),
                    precio_venta = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    costo = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    stock_actual = table.Column<int>(type: "int", nullable: false),
                    stock_minimo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__producto__FF341C0D9FC3B36F", x => x.id_producto);
                    table.ForeignKey(
                        name: "Fk_producto_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categoria",
                        principalColumn: "id_categoria");
                });

            migrationBuilder.CreateTable(
                name: "vendedor",
                columns: table => new
                {
                    id_vendedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    estado_activo = table.Column<bool>(type: "bit", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__vendedor__009303087E46AC98", x => x.id_vendedor);
                    table.ForeignKey(
                        name: "Fk_vendedor_rol",
                        column: x => x.id_rol,
                        principalTable: "rol",
                        principalColumn: "id_rol");
                });

            migrationBuilder.CreateTable(
                name: "butaca",
                columns: table => new
                {
                    id_butaca = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_sala = table.Column<int>(type: "int", nullable: false),
                    fila = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__butaca__EB757993F026BC06", x => x.id_butaca);
                    table.ForeignKey(
                        name: "FK_butaca_sala",
                        column: x => x.id_sala,
                        principalTable: "sala",
                        principalColumn: "id_sala");
                });

            migrationBuilder.CreateTable(
                name: "funcion",
                columns: table => new
                {
                    id_funcion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_pelicula = table.Column<int>(type: "int", nullable: false),
                    id_sala = table.Column<int>(type: "int", nullable: false),
                    fecha_funcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    hora_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    precio = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__funcion__F6DE6FC968CCA0E7", x => x.id_funcion);
                    table.ForeignKey(
                        name: "FK_Funcion_Pelicula",
                        column: x => x.id_pelicula,
                        principalTable: "pelicula",
                        principalColumn: "id_pelicula");
                    table.ForeignKey(
                        name: "FK_Funcion_Sala",
                        column: x => x.id_sala,
                        principalTable: "sala",
                        principalColumn: "id_sala");
                });

            migrationBuilder.CreateTable(
                name: "entrada_mercancia",
                columns: table => new
                {
                    id_entrada = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    folio_factura = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    costo_total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    fecha_entrada = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__entrada___167CD61B4AC8571F", x => x.id_entrada);
                    table.ForeignKey(
                        name: "Fk_entrada_producto",
                        column: x => x.id_producto,
                        principalTable: "producto",
                        principalColumn: "id_producto");
                });

            migrationBuilder.CreateTable(
                name: "venta",
                columns: table => new
                {
                    id_venta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_vendedor = table.Column<int>(type: "int", nullable: false),
                    fecha_venta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    metodo_pago = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__venta__459533BFF5DDCFEC", x => x.id_venta);
                    table.ForeignKey(
                        name: "Fk_venta_vendedor",
                        column: x => x.id_vendedor,
                        principalTable: "vendedor",
                        principalColumn: "id_vendedor");
                });

            migrationBuilder.CreateTable(
                name: "estado_butaca_funcion",
                columns: table => new
                {
                    id_estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_butaca = table.Column<int>(type: "int", nullable: false),
                    id_funcion = table.Column<int>(type: "int", nullable: false),
                    estato = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__estado_b__86989FB2D9668AAA", x => x.id_estado);
                    table.ForeignKey(
                        name: "FK_estado_butaca",
                        column: x => x.id_butaca,
                        principalTable: "butaca",
                        principalColumn: "id_butaca");
                });

            migrationBuilder.CreateTable(
                name: "preorden",
                columns: table => new
                {
                    id_preorden = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_qr = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    id_funcion = table.Column<int>(type: "int", nullable: false),
                    id_cliente = table.Column<int>(type: "int", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    vigencia = table.Column<int>(type: "int", nullable: false),
                    estado_activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__preorden__5AE45A7B832F24E8", x => x.id_preorden);
                    table.ForeignKey(
                        name: "FK_preorden_funcion",
                        column: x => x.id_funcion,
                        principalTable: "funcion",
                        principalColumn: "id_funcion");
                    table.ForeignKey(
                        name: "Fk_preorden_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                });

            migrationBuilder.CreateTable(
                name: "detalle_venta",
                columns: table => new
                {
                    id_detalle_venta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_venta = table.Column<int>(type: "int", nullable: false),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__detalle___5B265D47AC6CF849", x => x.id_detalle_venta);
                    table.ForeignKey(
                        name: "Fk_detalle_venta",
                        column: x => x.id_venta,
                        principalTable: "venta",
                        principalColumn: "id_venta");
                    table.ForeignKey(
                        name: "fk_detallev_producto",
                        column: x => x.id_producto,
                        principalTable: "producto",
                        principalColumn: "id_producto");
                });

            migrationBuilder.CreateTable(
                name: "detalle_preorden_boleto",
                columns: table => new
                {
                    id_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_preorden = table.Column<int>(type: "int", nullable: false),
                    caantidad = table.Column<int>(type: "int", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__detalle___4F1332DED0F6B61F", x => x.id_detalle);
                    table.ForeignKey(
                        name: "Fk_detalle_preorden",
                        column: x => x.id_preorden,
                        principalTable: "preorden",
                        principalColumn: "id_preorden");
                });

            migrationBuilder.CreateTable(
                name: "detalle_preorden_dulceria",
                columns: table => new
                {
                    id_detalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_preorden = table.Column<int>(type: "int", nullable: false),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__detalle___4F1332DE8F87539E", x => x.id_detalle);
                    table.ForeignKey(
                        name: "Fk_detalleD_preorden",
                        column: x => x.id_preorden,
                        principalTable: "preorden",
                        principalColumn: "id_preorden");
                    table.ForeignKey(
                        name: "Fk_detalle_producto",
                        column: x => x.id_producto,
                        principalTable: "producto",
                        principalColumn: "id_producto");
                });

            migrationBuilder.CreateIndex(
                name: "IX_butaca_id_sala",
                table: "butaca",
                column: "id_sala");

            migrationBuilder.CreateIndex(
                name: "UQ__cliente__2A586E0B75A62BAA",
                table: "cliente",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detalle_preorden_boleto_id_preorden",
                table: "detalle_preorden_boleto",
                column: "id_preorden");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_preorden_dulceria_id_preorden",
                table: "detalle_preorden_dulceria",
                column: "id_preorden");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_preorden_dulceria_id_producto",
                table: "detalle_preorden_dulceria",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_venta_id_producto",
                table: "detalle_venta",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_venta_id_venta",
                table: "detalle_venta",
                column: "id_venta");

            migrationBuilder.CreateIndex(
                name: "IX_entrada_mercancia_id_producto",
                table: "entrada_mercancia",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_estado_butaca_funcion_id_butaca",
                table: "estado_butaca_funcion",
                column: "id_butaca");

            migrationBuilder.CreateIndex(
                name: "IX_funcion_id_pelicula",
                table: "funcion",
                column: "id_pelicula");

            migrationBuilder.CreateIndex(
                name: "IX_funcion_id_sala",
                table: "funcion",
                column: "id_sala");

            migrationBuilder.CreateIndex(
                name: "IX_preorden_id_cliente",
                table: "preorden",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_preorden_id_funcion",
                table: "preorden",
                column: "id_funcion");

            migrationBuilder.CreateIndex(
                name: "IX_producto_id_categoria",
                table: "producto",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_vendedor_id_rol",
                table: "vendedor",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_venta_id_vendedor",
                table: "venta",
                column: "id_vendedor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalle_preorden_boleto");

            migrationBuilder.DropTable(
                name: "detalle_preorden_dulceria");

            migrationBuilder.DropTable(
                name: "detalle_venta");

            migrationBuilder.DropTable(
                name: "entrada_mercancia");

            migrationBuilder.DropTable(
                name: "estado_butaca_funcion");

            migrationBuilder.DropTable(
                name: "redes_autorizadas");

            migrationBuilder.DropTable(
                name: "preorden");

            migrationBuilder.DropTable(
                name: "venta");

            migrationBuilder.DropTable(
                name: "producto");

            migrationBuilder.DropTable(
                name: "butaca");

            migrationBuilder.DropTable(
                name: "funcion");

            migrationBuilder.DropTable(
                name: "cliente");

            migrationBuilder.DropTable(
                name: "vendedor");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "pelicula");

            migrationBuilder.DropTable(
                name: "sala");

            migrationBuilder.DropTable(
                name: "rol");
        }
    }
}
