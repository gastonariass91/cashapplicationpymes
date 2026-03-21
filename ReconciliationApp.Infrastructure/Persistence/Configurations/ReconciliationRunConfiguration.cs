using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationRunConfiguration : IEntityTypeConfiguration<ReconciliationRun>
{
    public void Configure(EntityTypeBuilder<ReconciliationRun> b)
    {
        b.ToTable("reconciliation_runs");

        b.HasKey(x => x.Id);

        b.Property(x => x.BatchRunId).HasColumnName("batch_run_id").IsRequired();
        b.Property(x => x.PublicRunId).HasColumnName("public_run_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");

        b.HasOne(x => x.BatchRun)
            .WithMany()
            .HasForeignKey(x => x.BatchRunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Cases)
            .WithOne(x => x.Run)
            .HasForeignKey(x => x.ReconciliationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.BatchRunId).IsUnique();
        b.HasIndex(x => x.PublicRunId).IsUnique();
    }
}
