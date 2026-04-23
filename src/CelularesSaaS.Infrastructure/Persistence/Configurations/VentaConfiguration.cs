using CelularesSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CelularesSaaS.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.TenantId, v.NumeroVenta }).IsUnique();
        builder.Property(v => v.NumeroVenta).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Subtotal).HasPrecision(18, 2);
        builder.Property(v => v.Descuento).HasPrecision(18, 2);
        builder.Property(v => v.Total).HasPrecision(18, 2);
        builder.Property(v => v.CostoTotal).HasPrecision(18, 2);
        builder.Property(v => v.Ganancia).HasPrecision(18, 2);
        builder.Property(v => v.CotizacionDolar).HasPrecision(18, 2);

        builder.HasOne(v => v.Cliente)
            .WithMany(c => c.Ventas)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(v => v.PartePago)
            .WithOne(p => p.Venta)
            .HasForeignKey<Venta>(v => v.PartePagoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.Items)
            .WithOne(i => i.Venta)
            .HasForeignKey(i => i.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Pagos)
            .WithOne(p => p.Venta)
            .HasForeignKey(p => p.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}