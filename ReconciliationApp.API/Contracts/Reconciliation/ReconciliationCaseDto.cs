namespace ReconciliationApp.API.Contracts.Reconciliation;

public sealed record ReconciliationCaseDto(
    string CaseId,
    int DebtRowNumber,
    int PaymentRowNumber,
    string Customer,
    decimal DebtAmount,
    decimal PaymentAmount,
    decimal Delta,
    string Rule,
    string Status,
    string Confidence,
    string MatchType,
    string Evidence,
    string Suggestion,
    string? ResolvedBy = null
);
