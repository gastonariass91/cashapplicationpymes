using System.Reflection;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.Preview;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Tests;

// ---------------------------------------------------------------------------
// Stubs manuales
// ---------------------------------------------------------------------------

file sealed class StubBatchRepository : IBatchRepository
{
    private readonly ReconciliationBatch? _batch;
    public StubBatchRepository(ReconciliationBatch? batch) => _batch = batch;

    public void Add(ReconciliationBatch batch) { }
    public Task<ReconciliationBatch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_batch);
    public Task<ReconciliationBatch?> GetByCompanyAndPeriodAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<ReconciliationBatch?>(null);
    public Task<bool> ExistsForPeriodAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult(false);
}

file sealed class StubBatchRunRepository : IBatchRunRepository
{
    private readonly BatchRun? _run;
    public StubBatchRunRepository(BatchRun? run) => _run = run;

    public Task<BatchRun?> GetByBatchAndRunNumberAsync(Guid batchId, int runNumber, CancellationToken ct = default)
        => Task.FromResult(_run);
    public Task AddAsync(BatchRun run, CancellationToken ct)
        => Task.CompletedTask;
}

file sealed class StubDebtRepository : IDebtRepository
{
    private readonly List<Debt> _debts;
    public StubDebtRepository(List<Debt> debts) => _debts = debts;

    public Task<List<Debt>> ListByCompanyAsync(Guid companyId, CancellationToken ct)
        => Task.FromResult(_debts);
    public Task<List<Debt>> ListOpenByCompanyAsync(Guid companyId, CancellationToken ct)
        => Task.FromResult(_debts);
    public Task<Debt?> GetByCompanyCustomerAndInvoiceAsync(Guid companyId, Guid customerId, string invoiceNumber, CancellationToken ct)
        => Task.FromResult<Debt?>(null);
    public Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct)
        => Task.CompletedTask;
    public Task AddAsync(Debt debt, CancellationToken ct)
        => Task.CompletedTask;
    public Task AddRangeAsync(IEnumerable<Debt> debts, CancellationToken ct)
        => Task.CompletedTask;
}

file sealed class StubPaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _payments;
    public StubPaymentRepository(List<Payment> payments) => _payments = payments;

    public Task<List<Payment>> ListByCompanyAsync(Guid companyId, CancellationToken ct)
        => Task.FromResult(_payments);
    public Task<List<Payment>> ListPendingByCompanyAsync(Guid companyId, CancellationToken ct)
        => Task.FromResult(_payments);
    public Task<Payment?> GetByCompanyAndPaymentNumberAsync(Guid companyId, string paymentNumber, CancellationToken ct)
        => Task.FromResult<Payment?>(null);
    public Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct)
        => Task.CompletedTask;
    public Task AddAsync(Payment payment, CancellationToken ct)
        => Task.CompletedTask;
    public Task AddRangeAsync(IEnumerable<Payment> payments, CancellationToken ct)
        => Task.CompletedTask;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

file static class Helpers
{
    private static readonly DateOnly Today = new(2026, 3, 15);

    public static (ReconciliationBatch batch, BatchRun run) MakeBatchAndRun(Guid companyId)
    {
        var batch = new ReconciliationBatch(
            companyId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        var run = batch.CreateNewRun();
        return (batch, run);
    }

    public static Customer MakeCustomer(Guid companyId, string customerKey)
        => new(companyId, customerKey, $"Cliente {customerKey}");

    /// <summary>
    /// Crea una Debt y le inyecta la navegación Customer via reflection,
    /// simulando lo que EF hace con Include() en producción.
    /// </summary>
    public static Debt MakeDebt(Guid companyId, Customer customer, string invoiceNumber, decimal amount)
    {
        var debt = new Debt(
            companyId:         companyId,
            customerId:        customer.Id,
            invoiceNumber:     invoiceNumber,
            issueDate:         Today,
            dueDate:           Today.AddDays(30),
            amount:            amount,
            currency:          "ARS",
            outstandingAmount: amount);

        // EF popula la propiedad de navegación internamente; la replicamos vía reflection.
        typeof(Debt)
            .GetProperty(nameof(Debt.Customer), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(debt, customer);

        return debt;
    }

    public static Payment MakePayment(Guid companyId, Customer customer, string paymentNumber, decimal amount)
        => new(
            companyId:     companyId,
            paymentNumber: paymentNumber,
            paymentDate:   Today,
            accountNumber: "ACC-001",
            amount:        amount,
            currency:      "ARS",
            customerId:    customer.Id);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public class ReconcilePreviewTests
{
    // 1 deuda + 1 pago mismo cliente y monto → 1 match
    [Fact]
    public async Task SingleMatch_WhenDebtAndPaymentMatch()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customer = Helpers.MakeCustomer(companyId, "CUST-01");

        var debts    = new List<Debt>    { Helpers.MakeDebt(companyId, customer, "INV-001", 1000m) };
        var payments = new List<Payment> { Helpers.MakePayment(companyId, customer, "PAY-001", 1000m) };

        var result = await RunPreview(batch, run, debts, payments);

        Assert.Single(result.matches);
        Assert.Empty(result.unmatchedDebtRowNumbers);
        Assert.Empty(result.unmatchedPaymentRowNumbers);
        Assert.Equal(1000m, result.matches[0].amount);
    }

    // Sin pagos → todas las deudas sin match
    [Fact]
    public async Task AllDebtsUnmatched_WhenNoPayments()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customer = Helpers.MakeCustomer(companyId, "CUST-01");

        var debts = new List<Debt>
        {
            Helpers.MakeDebt(companyId, customer, "INV-001", 500m),
            Helpers.MakeDebt(companyId, customer, "INV-002", 800m),
        };

        var result = await RunPreview(batch, run, debts, new List<Payment>());

        Assert.Empty(result.matches);
        Assert.Equal(2, result.unmatchedDebtRowNumbers.Count);
        Assert.Empty(result.unmatchedPaymentRowNumbers);
    }

    // Sin deudas → todos los pagos sin match
    [Fact]
    public async Task AllPaymentsUnmatched_WhenNoDebts()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customer = Helpers.MakeCustomer(companyId, "CUST-01");

        var payments = new List<Payment> { Helpers.MakePayment(companyId, customer, "PAY-001", 500m) };

        var result = await RunPreview(batch, run, new List<Debt>(), payments);

        Assert.Empty(result.matches);
        Assert.Empty(result.unmatchedDebtRowNumbers);
        Assert.Single(result.unmatchedPaymentRowNumbers);
    }

    // Monto diferente → no matchea
    [Fact]
    public async Task NoMatch_WhenAmountDiffers()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customer = Helpers.MakeCustomer(companyId, "CUST-01");

        var debts    = new List<Debt>    { Helpers.MakeDebt(companyId, customer, "INV-001", 1000m) };
        var payments = new List<Payment> { Helpers.MakePayment(companyId, customer, "PAY-001", 999m) };

        var result = await RunPreview(batch, run, debts, payments);

        Assert.Empty(result.matches);
        Assert.Single(result.unmatchedDebtRowNumbers);
        Assert.Single(result.unmatchedPaymentRowNumbers);
    }

    // Cliente diferente → no matchea
    [Fact]
    public async Task NoMatch_WhenCustomerDiffers()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customerA = Helpers.MakeCustomer(companyId, "CUST-A");
        var customerB = Helpers.MakeCustomer(companyId, "CUST-B");

        var debts    = new List<Debt>    { Helpers.MakeDebt(companyId, customerA, "INV-001", 500m) };
        var payments = new List<Payment> { Helpers.MakePayment(companyId, customerB, "PAY-001", 500m) };

        var result = await RunPreview(batch, run, debts, payments);

        Assert.Empty(result.matches);
    }

    // Sin filas → resultado vacío sin error
    [Fact]
    public async Task EmptyRows_ReturnsEmptyResult()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);

        var result = await RunPreview(batch, run, new List<Debt>(), new List<Payment>());

        Assert.Empty(result.matches);
        Assert.Empty(result.unmatchedDebtRowNumbers);
        Assert.Empty(result.unmatchedPaymentRowNumbers);
    }

    // Múltiples clientes → cada uno matchea con su pago correcto
    [Fact]
    public async Task MultipleCustomers_EachMatchesCorrectly()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customerA = Helpers.MakeCustomer(companyId, "CUST-A");
        var customerB = Helpers.MakeCustomer(companyId, "CUST-B");

        var debts = new List<Debt>
        {
            Helpers.MakeDebt(companyId, customerA, "INV-001", 100m),
            Helpers.MakeDebt(companyId, customerB, "INV-002", 200m),
        };
        var payments = new List<Payment>
        {
            Helpers.MakePayment(companyId, customerA, "PAY-001", 100m),
            Helpers.MakePayment(companyId, customerB, "PAY-002", 200m),
        };

        var result = await RunPreview(batch, run, debts, payments);

        Assert.Equal(2, result.matches.Count);
        Assert.Empty(result.unmatchedDebtRowNumbers);
        Assert.Empty(result.unmatchedPaymentRowNumbers);
    }

    // Mismo cliente, mismo monto, 2 deudas y 2 pagos → 2 matches
    [Fact]
    public async Task TwoMatches_SameCustomerSameAmount()
    {
        var companyId = Guid.NewGuid();
        var (batch, run) = Helpers.MakeBatchAndRun(companyId);
        var customer = Helpers.MakeCustomer(companyId, "CUST-01");

        var debts = new List<Debt>
        {
            Helpers.MakeDebt(companyId, customer, "INV-001", 200m),
            Helpers.MakeDebt(companyId, customer, "INV-002", 200m),
        };
        var payments = new List<Payment>
        {
            Helpers.MakePayment(companyId, customer, "PAY-001", 200m),
            Helpers.MakePayment(companyId, customer, "PAY-002", 200m),
        };

        var result = await RunPreview(batch, run, debts, payments);

        Assert.Equal(2, result.matches.Count);
        Assert.Empty(result.unmatchedDebtRowNumbers);
        Assert.Empty(result.unmatchedPaymentRowNumbers);
    }

    // ---------------------------------------------------------------------------
    // Helper privado
    // ---------------------------------------------------------------------------
    private static Task<ReconcilePreview.PreviewResult> RunPreview(
        ReconciliationBatch batch,
        BatchRun run,
        List<Debt> debts,
        List<Payment> payments)
    {
        return ReconcilePreview.ExecuteAsync(
            batch.Id,
            run.RunNumber,
            new StubBatchRepository(batch),
            new StubBatchRunRepository(run),
            new StubDebtRepository(debts),
            new StubPaymentRepository(payments),
            CancellationToken.None);
    }
}
