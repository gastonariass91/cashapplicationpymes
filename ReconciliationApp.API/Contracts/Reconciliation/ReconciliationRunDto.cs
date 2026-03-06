namespace ReconciliationApp.API.Contracts.Reconciliation;

public sealed record ReconciliationRunDto(
    RunSummaryDto Summary,
    IReadOnlyList<ReconciliationCaseDto> Cases
);
