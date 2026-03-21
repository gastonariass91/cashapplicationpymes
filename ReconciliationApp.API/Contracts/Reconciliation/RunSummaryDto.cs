namespace ReconciliationApp.API.Contracts.Reconciliation;

public sealed record RunSummaryDto(
    string RunId,
    string CompanyId,
    string CompanyName,
    string Period,
    string Status,
    int TotalCases,
    int ResolvedCases,
    int AutomaticCases,
    int PendingCases,
    int ExceptionCases,
    int DebtsRowsTotal,
    int PaymentsRowsTotal,
    decimal DebtsAmountTotal,
    decimal PaymentsAmountTotal
);
