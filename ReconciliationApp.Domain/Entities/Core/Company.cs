using ReconciliationApp.Domain.Enums;

namespace ReconciliationApp.Domain.Entities.Core;

public sealed class Company
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = default!;

    // Config inicial (MVP)
    public int AutoApplyScoreThreshold { get; private set; } = 90;

    // Tipado con enum en lugar de string libre
    public ReconciliationMode ReconciliationMode { get; private set; } = ReconciliationMode.Validation;

    private Company() { } // EF

    public Company(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Company name is required")
            : name.Trim();
    }

    public void SetAutoApplyScoreThreshold(int threshold)
    {
        if (threshold < 0 || threshold > 100)
            throw new ArgumentOutOfRangeException(nameof(threshold));

        AutoApplyScoreThreshold = threshold;
    }

    public void SetMode(ReconciliationMode mode)
    {
        // Al ser enum, el compilador garantiza valores válidos.
        // La guarda extra protege contra casts inválidos (ej: (ReconciliationMode)99).
        if (!Enum.IsDefined(typeof(ReconciliationMode), mode))
            throw new ArgumentException($"Invalid reconciliation mode: {mode}", nameof(mode));

        ReconciliationMode = mode;
    }
}
