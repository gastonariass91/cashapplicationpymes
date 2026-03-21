using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.Imports;
using ReconciliationApp.Domain.Entities.Reconciliation;
using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Batching
    public DbSet<ReconciliationBatch> Batches => Set<ReconciliationBatch>();
    public DbSet<BatchRun> BatchRuns => Set<BatchRun>();

    // Imports
    public DbSet<ImportRow> ImportRows => Set<ImportRow>();

    // Reconciliation
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();
    public DbSet<ReconciliationRun> ReconciliationRuns => Set<ReconciliationRun>();
    public DbSet<ReconciliationCase> ReconciliationCases => Set<ReconciliationCase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
