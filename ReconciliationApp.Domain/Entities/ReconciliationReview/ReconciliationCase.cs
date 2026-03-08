namespace ReconciliationApp.Domain.Entities.ReconciliationReview;

public sealed class ReconciliationCase
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ReconciliationRunId { get; private set; }

    public ReconciliationRun Run { get; private set; } = default!;

    public string CaseId { get; private set; } = default!;

    public int DebtRowNumber { get; private set; }

    public int PaymentRowNumber { get; private set; }

    public string Customer { get; private set; } = default!;

    public decimal DebtAmount { get; private set; }

    public decimal PaymentAmount { get; private set; }

    public decimal Delta { get; private set; }

    public string Rule { get; private set; } = default!;

    public string Status { get; private set; } = "pending";

    public string Confidence { get; private set; } = default!;

    public string MatchType { get; private set; } = default!;

    public string Evidence { get; private set; } = default!;

    public string Suggestion { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ReconciliationCase() { }

    public ReconciliationCase(
        Guid runId,
        string caseId,
        int debtRow,
        int paymentRow,
        string customer,
        decimal debtAmount,
        decimal paymentAmount,
        decimal delta,
        string rule,
        string status,
        string confidence,
        string matchType,
        string evidence,
        string suggestion)
    {
        ReconciliationRunId = runId;
        CaseId = caseId;
        DebtRowNumber = debtRow;
        PaymentRowNumber = paymentRow;
        Customer = customer;
        DebtAmount = debtAmount;
        PaymentAmount = paymentAmount;
        Delta = delta;
        Rule = rule;
        Status = status;
        Confidence = confidence;
        MatchType = matchType;
        Evidence = evidence;
        Suggestion = suggestion;
    }

    public void Accept()
    {
        Status = "ok";
        if (Confidence == "low")
            Confidence = "medium";

        Suggestion = "Aceptar";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkException()
    {
        Status = "exception";
        Suggestion = "Excepción";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
