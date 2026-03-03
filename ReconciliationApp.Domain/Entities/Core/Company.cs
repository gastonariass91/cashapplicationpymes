namespace ReconciliationApp.Domain.Entities.Core;

public sealed class Company
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = default!;

    // Config inicial (MVP)
    public int AutoApplyScoreThreshold { get; private set; } = 90;

    // Validation (default) / Automatic (luego)
    public string ReconciliationMode { get; private set; } = "Validation";

    private Company() { } // EF

    public Company(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Company name is required") : name.Trim();
    }

    public void SetAutoApplyScoreThreshold(int threshold)
    {
        if (threshold < 0 || threshold > 100) throw new ArgumentOutOfRangeException(nameof(threshold));
        AutoApplyScoreThreshold = threshold;
    }

    public void SetMode(string mode)
    {
        if (mode is not ("Validation" or "Automatic")) throw new ArgumentException("Invalid mode");
        ReconciliationMode = mode;
    }
}
