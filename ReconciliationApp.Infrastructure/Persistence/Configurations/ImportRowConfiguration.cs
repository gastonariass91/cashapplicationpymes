using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Infrastructure.Persistence.Configurations;

public sealed class ImportRowConfiguration : IEntityTypeConfiguration<ImportRow>
{
    public void Configure(EntityTypeBuilder<ImportRow> builder)
    {
        builder.ToTable("import_rows");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchRunId).HasColumnName("batch_run_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(x => x.RowNumber).HasColumnName("row_number").IsRequired();
        builder.Property(x => x.DataJson).HasColumnName("data_json").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.BatchRunId, x.Type, x.RowNumber }).IsUnique();
    }
}
