namespace ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;

public sealed record ReconcileMatchDto(
    int debtRowNumber,
    int paymentRowNumber,
    string customerId,
    decimal amount
);

public sealed record ReconcileRunResult(
    Guid batchRunId,
    DateTimeOffset? reconciledAt,
    int matchesSaved,
    IReadOnlyList<ReconcileMatchDto> matches,
    IReadOnlyList<int> unmatchedDebtRowNumbers,
    IReadOnlyList<int> unmatchedPaymentRowNumbers,
    bool alreadyReconciled
);
