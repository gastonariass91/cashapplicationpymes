namespace ReconciliationApp.Frontend.Contracts.Reconciliation;

public sealed class ApiRunSummaryDto
{
    public string RunId { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Period { get; set; } = "";
    public string Status { get; set; } = "";
    public int TotalCases { get; set; }
    public int ResolvedCases { get; set; }
    public int AutomaticCases { get; set; }
    public int PendingCases { get; set; }
    public int ExceptionCases { get; set; }
    public int DebtsRowsTotal { get; set; }
    public int PaymentsRowsTotal { get; set; }
    public decimal DebtsAmountTotal { get; set; }
    public decimal PaymentsAmountTotal { get; set; }
}
