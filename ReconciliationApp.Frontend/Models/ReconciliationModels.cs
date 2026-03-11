namespace ReconciliationApp.Frontend.Models;

public enum ReconciliationView { Auto, Pending, Exceptions }
public enum RowStatus { Ok, Pending, Exception }
public enum Confidence { Alta, Media, Baja }

// Renamed to avoid conflict with System.IO.MatchType
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
    string Suggestion
)
{
    public string Key => string.IsNullOrWhiteSpace(CaseId)
        ? $"{DebtRowNumber?.ToString() ?? "none"}-{PaymentRowNumber?.ToString() ?? "none"}"
        : CaseId;
}
