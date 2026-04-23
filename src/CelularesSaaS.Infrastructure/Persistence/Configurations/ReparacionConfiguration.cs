using CelularesSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CelularesSaaS.Infrastructure.Persistence.Configurations;

public class ReparacionConfiguration : IEntityTypeConfiguration<Reparacion>
{
    public void Configure(EntityTypeBuilder<Reparacion> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.TenantId, r.NumeroOrden }).IsUnique();
        builder.Property(r => r.NumeroOrden).HasMaxLength(20).IsRequired();
        builder.Property(r => r.ProblemaReportado).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.PresupuestoMonto).HasPrecision(18, 2);
        builder.Property(r => r.TotalCobrado).HasPrecision(18, 2);
        builder.Property(r => r.CostoRepuestos).HasPrecision(18, 2);
        builder.Property(r => r.ManoDeObra).HasPrecision(18, 2);

        builder.HasOne(r => r.Cliente).WithMany(c => c.Reparaciones)
            .HasForeignKey(r => r.ClienteId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Historial).WithOne(h => h.Reparacion)
            .HasForeignKey(h => h.ReparacionId).OnDelete(DeleteBehavior.Cascade);
    }
}
