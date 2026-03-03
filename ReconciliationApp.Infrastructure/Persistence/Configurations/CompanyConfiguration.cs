using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.Core;

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

        builder.Property(x => x.ReconciliationMode)
            .HasColumnName("reconciliation_mode")
            .HasMaxLength(30)
            .IsRequired();
    }
}
