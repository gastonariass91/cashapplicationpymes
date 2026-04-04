namespace ReconciliationApp.Domain.Entities.Core;

public sealed class Customer
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Obligatorio por tu decisión (CustomerKey en deuda)
    // A nivel de negocio hoy representa la clave fiscal del cliente.
    public string CustomerKey { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Email { get; private set; }

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Customer() { } // EF

    public Customer(Guid companyId, string customerKey, string name, string? email = null)
    {
        CompanyId = companyId;

        CustomerKey = string.IsNullOrWhiteSpace(customerKey)
            ? throw new ArgumentException("CustomerKey is required")
            : customerKey.Trim();

        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Customer name is required")
            : name.Trim();

        Email = NormalizeEmail(email);
    }

    public void UpdateName(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Customer name is required")
            : name.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateEmail(string? email)
    {
        Email = NormalizeEmail(email);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return email.Trim();
    }
}