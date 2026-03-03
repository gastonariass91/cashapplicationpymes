namespace ReconciliationApp.Domain.Entities.Core;

public sealed class Customer
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // Obligatorio por tu decisión (CustomerKey en deuda)
    public string CustomerKey { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    private Customer() { } // EF

    public Customer(Guid companyId, string customerKey, string name)
    {
        CompanyId = companyId;

        CustomerKey = string.IsNullOrWhiteSpace(customerKey)
            ? throw new ArgumentException("CustomerKey is required")
            : customerKey.Trim();

        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Customer name is required")
            : name.Trim();
    }
}
