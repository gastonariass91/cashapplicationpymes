namespace ReconciliationApp.Frontend.Models;
public enum ReconciliationView { Auto, Pending, Exceptions }
public enum RowStatus { Ok, Pending, Exception }
public enum Confidence { Alta, Media, Baja }
public enum ReconMatchType { Exact, Partial, Multi, Dup, Amb, NoMatch }
public sealed record ReconciliationRow(
    string CaseId,
    int? DebtRowNumber,
    int? PaymentRowNumber,
    string Customer,
    decimal DebtAmount,
    decimal PaymentAmount,
    decimal Delta,
    string Rule,
    RowStatus Status,
    Confidence Confidence,
    ReconMatchType Type,
    string Evidence,
    string Suggestion,
    string? ResolvedBy = null
)
{
    public string Key => string.IsNullOrWhiteSpace(CaseId)
        ? $"{DebtRowNumber?.ToString() ?? "none"}-{PaymentRowNumber?.ToString() ?? "none"}"
        : CaseId;
}
