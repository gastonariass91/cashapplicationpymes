using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ReconciliationBatch> Batches => Set<ReconciliationBatch>();
    public DbSet<BatchRun> BatchRuns => Set<BatchRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<ReconciliationApp.Infrastructure.Sql.RunNumberService.RunNumberRow>().HasNoKey();
        base.OnModelCreating(modelBuilder);
    }
}
