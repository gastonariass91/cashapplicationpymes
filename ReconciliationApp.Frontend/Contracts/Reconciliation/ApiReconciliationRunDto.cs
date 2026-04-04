namespace ReconciliationApp.Frontend.Contracts.Reconciliation;

public sealed class ApiReconciliationRunDto
{
    public ApiRunSummaryDto Summary { get; set; } = new();
    public List<ApiReconciliationCaseDto> Cases { get; set; } = [];
}
