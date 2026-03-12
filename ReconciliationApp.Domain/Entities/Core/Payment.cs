namespace ReconciliationApp.Domain.Entities.Core;

public sealed class Payment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    public string PaymentNumber { get; private set; } = default!;
    public DateOnly PaymentDate { get; private set; }

    public string AccountNumber { get; private set; } = default!;
    public string? PayerTaxId { get; private set; }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;

    public string Status { get; private set; } = "Available";

    public Guid? SourceBatchRunId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Payment() { } // EF

    public Payment(
        Guid companyId,
        string paymentNumber,
        DateOnly paymentDate,
        string accountNumber,
        decimal amount,
        string currency,
        Guid? customerId = null,
        string? payerTaxId = null,
        Guid? sourceBatchRunId = null)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        CompanyId = companyId;
        CustomerId = customerId;
        PaymentNumber = NormalizeRequired(paymentNumber, nameof(paymentNumber));
        PaymentDate = paymentDate;
        AccountNumber = NormalizeRequired(accountNumber, nameof(accountNumber));
        PayerTaxId = NormalizeOptional(payerTaxId);
        Amount = amount;
        Currency = NormalizeCurrency(currency);
        SourceBatchRunId = sourceBatchRunId;

        Status = customerId is null ? "Unidentified" : "Available";
    }

    public void AssignCustomer(Guid customerId)
    {
        CustomerId = customerId;

        if (Status == "Unidentified")
            Status = "Available";

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkApplied()
    {
        Status = "Applied";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPartiallyApplied()
    {
        Status = "PartiallyApplied";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRejected()
    {
        Status = "Rejected";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required", paramName);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = NormalizeRequired(currency, nameof(currency)).ToUpperInvariant();

        if (normalized.Length > 10)
            throw new ArgumentException("Invalid currency", nameof(currency));

        return normalized;
    }
}
