using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Enums;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationBatchConfiguration : IEntityTypeConfiguration<ReconciliationBatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationBatch> builder)
    {
        builder.ToTable("reconciliation_batches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(x => x.PeriodFrom)
            .HasColumnName("period_from")
            .IsRequired();

        builder.Property(x => x.PeriodTo)
            .HasColumnName("period_to")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CurrentRunNumber)
            .HasColumnName("current_run_number")
            .IsRequired();

        builder.HasMany(x => x.Runs)
            .WithOne()
            .HasForeignKey("batch_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CompanyId, x.PeriodFrom, x.PeriodTo })
            .IsUnique();
    }
}
