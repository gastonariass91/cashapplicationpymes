namespace ReconciliationApp.Domain.Entities.ReconciliationReview;

public sealed record ReviewRunTotals(
    int DebtsRowsTotal,
    int PaymentsRowsTotal,
    decimal DebtsAmountTotal,
    decimal PaymentsAmountTotal
);
