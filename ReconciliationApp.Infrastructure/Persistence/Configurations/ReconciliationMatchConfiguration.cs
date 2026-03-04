using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.Reconciliation;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationMatchConfiguration : IEntityTypeConfiguration<ReconciliationMatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationMatch> b)
    {
        b.ToTable("reconciliation_matches");

        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Amount).HasColumnType("numeric(18,2)");

        b.HasIndex(x => new { x.BatchRunId, x.DebtRowNumber }).IsUnique();
        b.HasIndex(x => new { x.BatchRunId, x.PaymentRowNumber }).IsUnique();

        b.HasOne(x => x.BatchRun)
            .WithMany()
            .HasForeignKey(x => x.BatchRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
