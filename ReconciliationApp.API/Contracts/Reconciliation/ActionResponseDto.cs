namespace ReconciliationApp.API.Contracts.Reconciliation;

public sealed record ActionResponseDto(
    bool Success,
    string Message,
    string RunStatus
);
