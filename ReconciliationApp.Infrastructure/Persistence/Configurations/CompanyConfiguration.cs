using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Enums;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AutoApplyScoreThreshold)
            .HasColumnName("auto_apply_score_threshold")
            .IsRequired();

        // Persiste el enum como string ("Validation" / "Automatic") en lugar de int,
        // así la DB es legible sin necesidad de un diccionario externo.
        builder.Property(x => x.ReconciliationMode)
            .HasColumnName("reconciliation_mode")
            .HasMaxLength(30)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ReconciliationMode>(v))
            .IsRequired();
    }
}
