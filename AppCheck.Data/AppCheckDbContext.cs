using Microsoft.EntityFrameworkCore;
using AppCheck.Data.Entities;

namespace AppCheck.Data;

public class AppCheckDbContext : DbContext
{
    public AppCheckDbContext(DbContextOptions<AppCheckDbContext> options) : base(options) { }

    public DbSet<Cuenta> Cuentas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<PuntoLectura> PuntosLectura { get; set; }
    public DbSet<RegistroJornada> RegistrosJornada { get; set; }
    public DbSet<AcuerdoFirmado> AcuerdosFirmados { get; set; }
    public DbSet<CodigoActivacion> CodigosActivacion { get; set; }
    public DbSet<BitacoraAuditoria> BitacoraAuditoria { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.HasKey(e => e.CuentaId);
            entity.Property(e => e.NombreComercial).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TipoCuenta).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SaldoActual).HasPrecision(18, 2);
            entity.HasIndex(e => e.RFC).IsUnique().HasFilter("[RFC] IS NOT NULL");
            entity.HasOne(e => e.CuentaPadre)
                  .WithMany(e => e.CuentasHijas)
                  .HasForeignKey(e => e.CuentaPadreId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Cuenta)
                  .WithMany(e => e.Usuarios)
                  .HasForeignKey(e => e.CuentaId);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.EmpleadoId);
            entity.Property(e => e.NumeroEmpleado).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PinRespaldo).IsRequired().HasMaxLength(10);
            entity.Property(e => e.MetodoLecturaPreferido).HasMaxLength(20);
            entity.HasOne(e => e.Cuenta)
                  .WithMany(e => e.Empleados)
                  .HasForeignKey(e => e.CuentaId);
        });

        modelBuilder.Entity<PuntoLectura>(entity =>
        {
            entity.HasKey(e => e.PuntoLecturaId);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TokenHash).IsRequired();
            entity.Property(e => e.EstadoLicencia).HasMaxLength(20);
            entity.HasOne(e => e.Cuenta)
                  .WithMany(e => e.PuntosLectura)
                  .HasForeignKey(e => e.CuentaId);
        });

        modelBuilder.Entity<RegistroJornada>(entity =>
        {
            entity.HasKey(e => e.RegistroId);
            entity.Property(e => e.TipoChecada).IsRequired().HasMaxLength(10);
            entity.Property(e => e.MetodoUsado).IsRequired().HasMaxLength(20);
            entity.Property(e => e.HashRegistro).IsRequired();
            entity.Property(e => e.Latitud).HasPrecision(9, 6);
            entity.Property(e => e.Longitud).HasPrecision(9, 6);
            entity.HasOne(e => e.Empleado)
                  .WithMany(e => e.RegistrosJornada)
                  .HasForeignKey(e => e.EmpleadoId);
            entity.HasOne(e => e.PuntoLectura)
                  .WithMany(e => e.RegistrosJornada)
                  .HasForeignKey(e => e.PuntoLecturaId);
        });

        modelBuilder.Entity<AcuerdoFirmado>(entity =>
        {
            entity.HasKey(e => e.AcuerdoId);
            entity.Property(e => e.DocumentoUrl).IsRequired();
            entity.HasOne(e => e.Empleado)
                  .WithMany(e => e.AcuerdosFirmados)
                  .HasForeignKey(e => e.EmpleadoId);
        });

        modelBuilder.Entity<CodigoActivacion>(entity =>
        {
            entity.HasKey(e => e.CodigoId);
            entity.Property(e => e.CodigoHash).IsRequired();
            entity.Property(e => e.Tipo).HasMaxLength(20);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.HasOne(e => e.Cuenta)
                  .WithMany()
                  .HasForeignKey(e => e.CuentaId);
            entity.HasOne(e => e.PuntoLectura)
                  .WithMany()
                  .HasForeignKey(e => e.PuntoLecturaId);
        });

        modelBuilder.Entity<BitacoraAuditoria>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.Accion).IsRequired().HasMaxLength(200);
        });
    }
}
