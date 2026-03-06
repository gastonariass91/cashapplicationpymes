namespace ReconciliationApp.Frontend.Contracts.Reconciliation;

public sealed class ApiReconciliationCaseDto
{
    public string CaseId { get; set; } = "";
    public int DebtRowNumber { get; set; }
    public int PaymentRowNumber { get; set; }
    public string Customer { get; set; } = "";
    public decimal DebtAmount { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal Delta { get; set; }
    public string Rule { get; set; } = "";
    public string Status { get; set; } = "";
    public string Confidence { get; set; } = "";
    public string MatchType { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string Suggestion { get; set; } = "";
}
