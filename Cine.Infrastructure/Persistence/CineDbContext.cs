using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Cine.Domain.Entities;
using Cine.Application.Interfaces;

namespace Cine.Infrastructure.Persistence;

public partial class CineDbContext : DbContext, ICineDbContext
{
    public CineDbContext()
    {
    }

    public CineDbContext(DbContextOptions<CineDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Butaca> Butacas { get; set; }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<DetallePreordenBoleto> DetallePreordenBoletos { get; set; }

    public virtual DbSet<DetallePreordenDulcerium> DetallePreordenDulceria { get; set; }

    public virtual DbSet<DetalleVentum> DetalleVenta { get; set; }

    public virtual DbSet<EntradaMercancium> EntradaMercancia { get; set; }

    public virtual DbSet<EstadoButacaFuncion> EstadoButacaFuncions { get; set; }

    public virtual DbSet<Funcion> Funcions { get; set; }

    public virtual DbSet<InventarioStockFraccionado> InventarioStockFraccionados { get; set; }

    public virtual DbSet<Pelicula> Peliculas { get; set; }

    public virtual DbSet<Preorden> Preordens { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<RedesAutorizada> RedesAutorizadas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Sala> Salas { get; set; }

    public virtual DbSet<Vendedor> Vendedors { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    public virtual DbSet<Proveedor> Proveedores { get; set; }

    public virtual DbSet<FacturaProveedor> FacturasProveedor { get; set; }

    public virtual DbSet<CanjePreorden> CanjePreordens { get; set; }

    public virtual DbSet<PagoVenta> PagoVentas { get; set; }

    public virtual DbSet<Sucursal> Sucursales { get; set; }

    public virtual DbSet<ConfiguracionSeguridad> ConfiguracionesSeguridad { get; set; }

    public virtual DbSet<TurnoLaboral> TurnosLaborales { get; set; }

    public virtual DbSet<TematicaSitio> TematicasSitio { get; set; }

    public virtual DbSet<PlantillaCss> PlantillasCss { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Butaca>(entity =>
        {
            entity.HasKey(e => e.IdButaca).HasName("PK__butaca__EB757993F026BC06");

            entity.ToTable("butaca");

            entity.Property(e => e.IdButaca).HasColumnName("id_butaca");
            entity.Property(e => e.Fila)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("fila");
            entity.Property(e => e.IdSala).HasColumnName("id_sala");
            entity.Property(e => e.Numero).HasColumnName("numero");

            entity.HasOne(d => d.IdSalaNavigation).WithMany(p => p.Butacas)
                .HasForeignKey(d => d.IdSala)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_butaca_sala");
        });

        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK__categori__CD54BC5AB14327EA");

            entity.ToTable("categoria");

            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente).HasName("PK__cliente__677F38F5A756C86F");

            entity.ToTable("cliente");

            entity.HasIndex(e => e.Correo, "UQ__cliente__2A586E0B75A62BAA").IsUnique();

            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("correo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.NumeroTelefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("numero_telefono");
        });

        modelBuilder.Entity<DetallePreordenBoleto>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK__detalle___4F1332DED0F6B61F");

            entity.ToTable("detalle_preorden_boleto");

            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.Caantidad).HasColumnName("caantidad");
            entity.Property(e => e.IdPreorden).HasColumnName("id_preorden");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("subtotal");

            entity.HasOne(d => d.IdPreordenNavigation).WithMany(p => p.DetallePreordenBoletos)
                .HasForeignKey(d => d.IdPreorden)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_detalle_preorden");
        });

        modelBuilder.Entity<DetallePreordenDulcerium>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK__detalle___4F1332DE8F87539E");

            entity.ToTable("detalle_preorden_dulceria");

            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdPreorden).HasColumnName("id_preorden");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");

            entity.HasOne(d => d.IdPreordenNavigation).WithMany(p => p.DetallePreordenDulceria)
                .HasForeignKey(d => d.IdPreorden)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_detalleD_preorden");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetallePreordenDulceria)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_detalle_producto");
        });

        modelBuilder.Entity<DetalleVentum>(entity =>
        {
            entity.HasKey(e => e.IdDetalleVenta).HasName("PK__detalle___5B265D47AC6CF849");

            entity.ToTable("detalle_venta");

            entity.Property(e => e.IdDetalleVenta).HasColumnName("id_detalle_venta");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdVenta).HasColumnName("id_venta");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_detallev_producto");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_detalle_venta");
        });

        modelBuilder.Entity<EntradaMercancium>(entity =>
        {
            entity.HasKey(e => e.IdEntrada).HasName("PK__entrada___167CD61B4AC8571F");

            entity.ToTable("entrada_mercancia");

            entity.Property(e => e.IdEntrada).HasColumnName("id_entrada");
            entity.Property(e => e.CostoTotal)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("costo_total");
            entity.Property(e => e.FechaEntrada).HasColumnName("fecha_entrada");
            entity.Property(e => e.FolioFactura)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("folio_factura");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");

            entity.Property(e => e.IdFactura).HasColumnName("id_factura");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad").HasDefaultValue(1);
            entity.Property(e => e.CostoUnitario)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("costo_unitario");
            entity.Property(e => e.CantidadStockAgregada).HasColumnName("cantidad_stock_agregada").HasDefaultValue(1);

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.EntradaMercancia)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_entrada_producto");

            entity.HasOne(d => d.IdFacturaNavigation).WithMany(p => p.Entradas)
                .HasForeignKey(d => d.IdFactura)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_entrada_factura");
        });

        modelBuilder.Entity<EstadoButacaFuncion>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PK__estado_b__86989FB2D9668AAA");

            entity.ToTable("estado_butaca_funcion");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.Estato).HasColumnName("estato");
            entity.Property(e => e.IdButaca).HasColumnName("id_butaca");
            entity.Property(e => e.IdFuncion).HasColumnName("id_funcion");

            entity.HasOne(d => d.IdButacaNavigation).WithMany(p => p.EstadoButacaFuncions)
                .HasForeignKey(d => d.IdButaca)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_estado_butaca");
        });

        modelBuilder.Entity<Funcion>(entity =>
        {
            entity.HasKey(e => e.IdFuncion).HasName("PK__funcion__F6DE6FC968CCA0E7");

            entity.ToTable("funcion");

            entity.Property(e => e.IdFuncion).HasColumnName("id_funcion");
            entity.Property(e => e.FechaFuncion).HasColumnName("fecha_funcion");
            entity.Property(e => e.HoraInicio).HasColumnName("hora_inicio");
            entity.Property(e => e.IdPelicula).HasColumnName("id_pelicula");
            entity.Property(e => e.IdSala).HasColumnName("id_sala");
            entity.Property(e => e.Precio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("precio");

            entity.HasOne(d => d.IdPeliculaNavigation).WithMany(p => p.Funcions)
                .HasForeignKey(d => d.IdPelicula)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Funcion_Pelicula");

            entity.HasOne(d => d.IdSalaNavigation).WithMany(p => p.Funcions)
                .HasForeignKey(d => d.IdSala)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Funcion_Sala");
        });

        modelBuilder.Entity<InventarioStockFraccionado>(entity =>
        {
            entity.HasKey(e => e.IdStockFraccionado).HasName("PK_inventario_stock_fraccionado");

            entity.ToTable("inventario_stock_fraccionado");

            entity.Property(e => e.IdStockFraccionado).HasColumnName("id_stock_fraccionado");
            entity.Property(e => e.IdProductoMaestro).HasColumnName("id_producto_maestro");
            entity.Property(e => e.IdProductoFraccion).HasColumnName("id_producto_fraccion");
            entity.Property(e => e.FactorFraccion).HasColumnName("factor_fraccion");
            entity.Property(e => e.StockFraccionesActual).HasColumnName("stock_fracciones_actual");
            entity.Property(e => e.StockFraccionesMinimo).HasColumnName("stock_fracciones_minimo");
            entity.Property(e => e.UnidadMedidaFraccion)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("unidad_medida_fraccion");
            entity.Property(e => e.UnidadMedidaMaestra)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("unidad_medida_maestra");
            entity.Property(e => e.UltimaActualizacion).HasColumnName("ultima_actualizacion");

            entity.HasOne(d => d.IdProductoMaestroNavigation)
                .WithMany(p => p.StockFraccionadoComoMaestro)
                .HasForeignKey(d => d.IdProductoMaestro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_fraccionado_producto_maestro");

            entity.HasOne(d => d.IdProductoFraccionNavigation)
                .WithMany(p => p.StockFraccionadoComoFraccion)
                .HasForeignKey(d => d.IdProductoFraccion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_fraccionado_producto_fraccion");
        });

        modelBuilder.Entity<Pelicula>(entity =>
        {
            entity.HasKey(e => e.IdPelicula).HasName("PK__pelicula__B5017F4D9F56E1A2");

            entity.ToTable("pelicula");

            entity.Property(e => e.IdPelicula).HasColumnName("id_pelicula");
            entity.Property(e => e.Clasificacion)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("clasificacion");
            entity.Property(e => e.Director)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("director");
            entity.Property(e => e.Duracion).HasColumnName("duracion");
            entity.Property(e => e.PosterImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("poster_image");
            entity.Property(e => e.Sinopsis)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("sinopsis");
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .HasColumnName("titulo");
        });

        modelBuilder.Entity<Preorden>(entity =>
        {
            entity.HasKey(e => e.IdPreorden).HasName("PK__preorden__5AE45A7B832F24E8");

            entity.ToTable("preorden");

            entity.Property(e => e.IdPreorden).HasColumnName("id_preorden");
            entity.Property(e => e.CodigoQr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("codigo_qr");
            entity.Property(e => e.EstadoActivo).HasColumnName("estado_activo");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdFuncion).HasColumnName("id_funcion");
            entity.Property(e => e.Vigencia).HasColumnName("vigencia");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Preordens)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_preorden_cliente");

            entity.HasOne(d => d.IdFuncionNavigation).WithMany(p => p.Preordens)
                .HasForeignKey(d => d.IdFuncion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_preorden_funcion");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK__producto__FF341C0D9FC3B36F");

            entity.ToTable("producto");

            entity.HasIndex(e => e.CodigoBarras, "IX_producto_codigo_barras");

            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(255)
                .HasColumnName("codigo_barras");
            entity.Property(e => e.Costo)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("costo");
            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.PrecioVenta)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("precio_venta");
            entity.Property(e => e.StockActual).HasColumnName("stock_actual");
            entity.Property(e => e.StockMinimo).HasColumnName("stock_minimo");
            entity.Property(e => e.FactorEquivalencia).HasColumnName("factor_equivalencia").HasDefaultValue(1);
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_producto_categoria");
        });

        modelBuilder.Entity<RedesAutorizada>(entity =>
        {
            entity.HasKey(e => e.IdRed).HasName("PK__redes_au__6ABE6F0BF6D6E259");

            entity.ToTable("redes_autorizadas");

            entity.Property(e => e.IdRed).HasColumnName("id_red");
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("direccion_ip");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK__rol__6ABCB5E0179135A5");

            entity.ToTable("rol");

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("descripcion");
            entity.Property(e => e.NombreRol)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre_rol");
        });

        modelBuilder.Entity<Sala>(entity =>
        {
            entity.HasKey(e => e.IdSala).HasName("PK__sala__D18B015B01491D56");

            entity.ToTable("sala");

            entity.Property(e => e.IdSala).HasColumnName("id_sala");
            entity.Property(e => e.CapacidadTotal).HasColumnName("capacidad_total");
            entity.Property(e => e.NombreSala)
                .HasMaxLength(255)
                .HasColumnName("nombre_sala");
            entity.Property(e => e.TipoPantalla)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tipo_pantalla");
        });

        modelBuilder.Entity<Vendedor>(entity =>
        {
            entity.HasKey(e => e.IdVendedor).HasName("PK__vendedor__009303087E46AC98");

            entity.ToTable("vendedor");

            entity.Property(e => e.IdVendedor).HasColumnName("id_vendedor");
            entity.Property(e => e.EstadoActivo).HasColumnName("estado_activo");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Vendedors)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_vendedor_rol");
        });

        modelBuilder.Entity<Ventum>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("PK__venta__459533BFF5DDCFEC");

            entity.ToTable("venta");

            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.FechaVenta).HasColumnName("fecha_venta");
            entity.Property(e => e.IdVendedor).HasColumnName("id_vendedor");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("metodo_pago");
            entity.Property(e => e.Total)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_venta_vendedor");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor).HasName("PK__proveedo__8D3C3E3B");

            entity.ToTable("proveedor");

            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Rfc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("rfc");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("telefono");
            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("contacto");
            entity.Property(e => e.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<FacturaProveedor>(entity =>
        {
            entity.HasKey(e => e.IdFactura).HasName("PK__facturas__8A7E01E2");

            entity.ToTable("facturas_proveedor");

            entity.Property(e => e.IdFactura).HasColumnName("id_factura");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Folio)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("folio");
            entity.Property(e => e.TotalPagado)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_pagado");
            entity.Property(e => e.FechaRegistro)
                .HasColumnName("fecha_registro")
                .HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IdVendedor).HasColumnName("id_vendedor");

            entity.HasIndex(e => new { e.IdProveedor, e.Folio }, "UQ_facturas_proveedor_folio").IsUnique();

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_factura_proveedor");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany()
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_factura_vendedor");
        });

        modelBuilder.Entity<CanjePreorden>(entity =>
        {
            entity.HasKey(e => e.IdCanje).HasName("PK_canje_preorden");

            entity.ToTable("canje_preorden");

            entity.Property(e => e.IdCanje).HasColumnName("id_canje");
            entity.Property(e => e.IdPreorden).HasColumnName("id_preorden");
            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.IdVendedor).HasColumnName("id_vendedor");
            entity.Property(e => e.CodigoQr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("codigo_qr");
            entity.Property(e => e.FechaCanje).HasColumnName("fecha_canje");
            entity.Property(e => e.MontoCanjeado)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("monto_canjeado");

            entity.HasOne(d => d.IdPreordenNavigation).WithMany(p => p.CanjePreordens)
                .HasForeignKey(d => d.IdPreorden)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_canje_preorden_preorden");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.CanjePreordens)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_canje_preorden_venta");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany()
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_canje_preorden_vendedor");
        });

        modelBuilder.Entity<PagoVenta>(entity =>
        {
            entity.HasKey(e => e.IdPago).HasName("PK_pago_venta");

            entity.ToTable("pago_venta");

            entity.Property(e => e.IdPago).HasColumnName("id_pago");
            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("metodo_pago");
            entity.Property(e => e.MontoTotal)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("monto_total");
            entity.Property(e => e.MontoRecibido)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("monto_recibido");
            entity.Property(e => e.Cambio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("cambio");
            entity.Property(e => e.FolioAutorizacion)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("folio_autorizacion");
            entity.Property(e => e.CodigoBarrasTicket)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("codigo_barras_ticket");
            entity.Property(e => e.RfcEmisor)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("rfc_emisor");
            entity.Property(e => e.FechaPago).HasColumnName("fecha_pago");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.PagoVentas)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_venta_venta");
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Sucursales");

            entity.ToTable("Sucursales");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("Nombre");
            entity.Property(e => e.IpDefecto)
                .HasMaxLength(45)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("IpDefecto");
        });

        modelBuilder.Entity<ConfiguracionSeguridad>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Configuracion_Seguridad");

            entity.ToTable("Configuracion_Seguridad");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.SucursalId).HasColumnName("SucursalId");
            entity.Property(e => e.IpAutorizada)
                .HasMaxLength(45)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("IpAutorizada");
            entity.Property(e => e.HorarioInicioCajeros)
                .HasColumnType("time")
                .HasColumnName("HorarioInicioCajeros");
            entity.Property(e => e.HorarioFinCajeros)
                .HasColumnType("time")
                .HasColumnName("HorarioFinCajeros");
            entity.Property(e => e.AutorizarExcepcionHorasExtras)
                .HasDefaultValue(false)
                .HasColumnName("AutorizarExcepcionHorasExtras");
            entity.Property(e => e.FechaUltimaModificacion)
                .HasColumnType("datetime")
                .HasColumnName("FechaUltimaModificacion");
            entity.Property(e => e.UsuarioModificoId).HasColumnName("UsuarioModificoId");

            entity.HasOne(d => d.SucursalNavigation)
                .WithMany(p => p.ConfiguracionesSeguridad)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConfiguracionSeguridad_Sucursal");
        });

        modelBuilder.Entity<TurnoLaboral>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Turnos_Laborales");

            entity.ToTable("Turnos_Laborales");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.NombreTurno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("NombreTurno");
            entity.Property(e => e.HoraInicio)
                .HasColumnType("time")
                .HasColumnName("HoraInicio");
            entity.Property(e => e.HoraFin)
                .HasColumnType("time")
                .HasColumnName("HoraFin");
        });

        modelBuilder.Entity<TematicaSitio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Tematicas_Sitio");

            entity.ToTable("Tematicas_Sitio");

            entity.HasIndex(e => e.NombreTematica, "UQ_Tematicas_Sitio_NombreTematica").IsUnique();

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.NombreTematica)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("NombreTematica");
            entity.Property(e => e.ClaveIdentificadora)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("ClaveIdentificadora");
            entity.Property(e => e.EsActiva)
                .HasDefaultValue(false)
                .HasColumnName("EsActiva");
            entity.Property(e => e.FechaActivacion)
                .HasColumnType("datetime")
                .HasColumnName("FechaActivacion");
        });

        modelBuilder.Entity<PlantillaCss>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Plantillas_CSS");

            entity.ToTable("Plantillas_CSS");

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.TematicaId).HasColumnName("TematicaId");
            entity.Property(e => e.RutaArchivoCss)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("RutaArchivoCss");
            entity.Property(e => e.PaletaColoresJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired()
                .HasColumnName("PaletaColoresJson");
            entity.Property(e => e.Tipografia)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired()
                .HasColumnName("Tipografia");

            entity.HasOne(d => d.TematicaNavigation)
                .WithMany(p => p.PlantillasCss)
                .HasForeignKey(d => d.TematicaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PlantillasCss_Tematica");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    private static void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Método vacío intencionalmente. Reservado para futura configuración.
    }
}
