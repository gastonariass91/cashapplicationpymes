using System.Text.Json;

namespace ReconciliationApp.Application.Features.Imports;

public static class ImportRowParser
{
    public sealed record DebtImportRecord(
        string CustomerId,
        decimal Amount
    );

    public sealed record PaymentImportRecord(
        string CustomerId,
        decimal Amount
    );

    public static DebtImportRecord ParseDebt(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new DebtImportRecord(
            CustomerId: GetRequiredString(root, "customer_id"),
            Amount: GetRequiredDecimal(root, "amount")
        );
    }

    public static PaymentImportRecord ParsePayment(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new PaymentImportRecord(
            CustomerId: GetOptionalString(root, "payer_tax_id")
                     ?? GetOptionalString(root, "customer_id")
                     ?? "",
            Amount: GetRequiredDecimal(root, "amount")
        );
    }

    public static decimal ExtractAmount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("amount", out var amountElement))
            return 0m;

        if (amountElement.ValueKind == JsonValueKind.Number)
            return amountElement.GetDecimal();

        if (decimal.TryParse(amountElement.GetString(), out var amount))
            return amount;

        return 0m;
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            throw new InvalidOperationException($"Missing required property '{propertyName}'.");

        var result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException($"Property '{propertyName}' is required.");

        return result.Trim();
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        var result = value.GetString();
        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    private static decimal GetRequiredDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            throw new InvalidOperationException($"Missing required property '{propertyName}'.");

        if (value.ValueKind == JsonValueKind.Number)
            return value.GetDecimal();

        var raw = value.GetString();
        if (decimal.TryParse(raw, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Property '{propertyName}' must be a valid decimal.");
    }
}
