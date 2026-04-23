using CelularesSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CelularesSaaS.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.TenantId, p.Codigo }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.CodigoBarras })
            .IsUnique()
            .HasFilter("\"CodigoBarras\" IS NOT NULL");

        builder.Property(p => p.Codigo).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CodigoBarras).HasMaxLength(50);
        builder.Property(p => p.Marca).HasMaxLength(100);
        builder.Property(p => p.Modelo).HasMaxLength(150);
        builder.Property(p => p.ImagenUrl).HasMaxLength(500);
        builder.Property(p => p.ImagenPublicId).HasMaxLength(200);

        builder.Property(p => p.PrecioCompraARS).HasPrecision(18, 2);
        builder.Property(p => p.PrecioCompraUSD).HasPrecision(18, 2);
        builder.Property(p => p.PrecioVentaARS).HasPrecision(18, 2);
        builder.Property(p => p.PrecioVentaUSD).HasPrecision(18, 2);

        builder.HasOne(p => p.Proveedor)
            .WithMany()
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Movimientos)
            .WithOne(m => m.Producto)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
