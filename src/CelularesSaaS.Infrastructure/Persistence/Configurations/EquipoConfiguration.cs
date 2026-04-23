using CelularesSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CelularesSaaS.Infrastructure.Persistence.Configurations;

public class EquipoConfiguration : IEntityTypeConfiguration<Equipo>
{
    public void Configure(EntityTypeBuilder<Equipo> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Imei }).IsUnique();
        builder.Property(e => e.Marca).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Modelo).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Capacidad).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Imei).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Imei2).HasMaxLength(20);
        builder.Property(e => e.PrecioCompra).HasPrecision(18, 2);
        builder.Property(e => e.PrecioVentaSugerido).HasPrecision(18, 2);
        builder.Property(e => e.CotizacionDolarCompra).HasPrecision(18, 2);

        builder.HasOne(e => e.Proveedor).WithMany(p => p.Equipos)
            .HasForeignKey(e => e.ProveedorId).OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ClienteOrigen).WithMany(c => c.EquiposVendidos)
            .HasForeignKey(e => e.ClienteOrigenId).OnDelete(DeleteBehavior.SetNull);

        // PartePago: Equipo es el dependiente (tiene la FK PartePagoId)
        builder.HasOne(e => e.PartePago)
            .WithOne(p => p.EquipoGenerado)
            .HasForeignKey<Equipo>(e => e.PartePagoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Historial).WithOne(h => h.Equipo)
            .HasForeignKey(h => h.EquipoId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Reparaciones).WithOne(r => r.Equipo)
            .HasForeignKey(r => r.EquipoId).OnDelete(DeleteBehavior.SetNull);
    }
}