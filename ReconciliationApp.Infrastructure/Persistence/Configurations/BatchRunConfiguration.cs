using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class BatchRunConfiguration : IEntityTypeConfiguration<BatchRun>
{
    public void Configure(EntityTypeBuilder<BatchRun> builder)
    {
        builder.ToTable("batch_runs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchId)
            .HasColumnName("batch_id")
            .IsRequired();

        builder.Property(x => x.RunNumber)
            .HasColumnName("run_number")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new { x.BatchId, x.RunNumber })
            .IsUnique();
    }
}
