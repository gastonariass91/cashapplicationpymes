using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.Imports;
using ReconciliationApp.Domain.Entities.Reconciliation;

namespace ReconciliationApp.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();

    // Batching
    public DbSet<ReconciliationBatch> Batches => Set<ReconciliationBatch>();
    public DbSet<BatchRun> BatchRuns => Set<BatchRun>();

    // Imports
    public DbSet<ImportRow> ImportRows => Set<ImportRow>();

    // Reconciliation
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
