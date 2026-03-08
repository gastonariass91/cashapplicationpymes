using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationCaseConfiguration : IEntityTypeConfiguration<ReconciliationCase>
{
    public void Configure(EntityTypeBuilder<ReconciliationCase> b)
    {
        b.ToTable("reconciliation_cases");

        b.HasKey(x => x.Id);

        b.Property(x => x.ReconciliationRunId).HasColumnName("reconciliation_run_id").IsRequired();
        b.Property(x => x.CaseId).HasColumnName("case_id").HasMaxLength(64).IsRequired();

        b.Property(x => x.DebtRowNumber).HasColumnName("debt_row_number").IsRequired();
        b.Property(x => x.PaymentRowNumber).HasColumnName("payment_row_number").IsRequired();

        b.Property(x => x.Customer).HasColumnName("customer").HasMaxLength(128).IsRequired();
        b.Property(x => x.DebtAmount).HasColumnName("debt_amount").HasColumnType("numeric(18,2)").IsRequired();
        b.Property(x => x.PaymentAmount).HasColumnName("payment_amount").HasColumnType("numeric(18,2)").IsRequired();
        b.Property(x => x.Delta).HasColumnName("delta").HasColumnType("numeric(18,2)").IsRequired();

        b.Property(x => x.Rule).HasColumnName("rule").HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        b.Property(x => x.Confidence).HasColumnName("confidence").HasMaxLength(32).IsRequired();
        b.Property(x => x.MatchType).HasColumnName("match_type").HasMaxLength(32).IsRequired();
        b.Property(x => x.Evidence).HasColumnName("evidence").HasMaxLength(512).IsRequired();
        b.Property(x => x.Suggestion).HasColumnName("suggestion").HasMaxLength(128).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        b.HasIndex(x => new { x.ReconciliationRunId, x.CaseId }).IsUnique();
    }
}
