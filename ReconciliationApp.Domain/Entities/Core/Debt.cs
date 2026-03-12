namespace ReconciliationApp.Domain.Entities.Core;

public sealed class Debt
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = default!;

    public string InvoiceNumber { get; private set; } = default!;

    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public decimal OutstandingAmount { get; private set; }

    public string Status { get; private set; } = "Open";

    public Guid? SourceBatchRunId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Debt() { } // EF

    public Debt(
        Guid companyId,
        Guid customerId,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        decimal amount,
        string currency,
        decimal outstandingAmount,
        Guid? sourceBatchRunId = null)
    {
        if (dueDate < issueDate)
            throw new ArgumentException("DueDate must be >= IssueDate");

        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (outstandingAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(outstandingAmount));

        if (outstandingAmount > amount)
            throw new ArgumentException("OutstandingAmount must be <= Amount");

        CompanyId = companyId;
        CustomerId = customerId;
        InvoiceNumber = NormalizeRequired(invoiceNumber, nameof(invoiceNumber));
        IssueDate = issueDate;
        DueDate = dueDate;
        Amount = amount;
        Currency = NormalizeCurrency(currency);
        OutstandingAmount = outstandingAmount;
        SourceBatchRunId = sourceBatchRunId;

        Status = CalculateStatus(amount, outstandingAmount);
    }

    public void UpdateOutstandingAmount(decimal outstandingAmount)
    {
        if (outstandingAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(outstandingAmount));

        if (outstandingAmount > Amount)
            throw new ArgumentException("OutstandingAmount must be <= Amount");

        OutstandingAmount = outstandingAmount;
        Status = CalculateStatus(Amount, OutstandingAmount);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkWrittenOff()
    {
        Status = "WrittenOff";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDisputed()
    {
        Status = "Disputed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required", paramName);

        return value.Trim();
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = NormalizeRequired(currency, nameof(currency)).ToUpperInvariant();

        if (normalized.Length > 10)
            throw new ArgumentException("Invalid currency", nameof(currency));

        return normalized;
    }

    private static string CalculateStatus(decimal amount, decimal outstandingAmount)
    {
        if (outstandingAmount == 0m) return "Paid";
        if (outstandingAmount < amount) return "PartiallyPaid";
        return "Open";
    }
}
