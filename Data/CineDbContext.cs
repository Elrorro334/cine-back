using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using cine_back.Models;

namespace cine_back.Data;

public partial class CineDbContext : DbContext
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

    public virtual DbSet<Pelicula> Peliculas { get; set; }

    public virtual DbSet<Preorden> Preordens { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<RedesAutorizada> RedesAutorizadas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Sala> Salas { get; set; }

    public virtual DbSet<Vendedor> Vendedors { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Data Source=localhost,1433;Initial Catalog=cineDataBase;User ID=sa;Password=Admsis123;Encrypt=False;TrustServerCertificate=True;");

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

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.EntradaMercancia)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_entrada_producto");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
