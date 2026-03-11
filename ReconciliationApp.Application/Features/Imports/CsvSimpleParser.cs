using System.Text.Json;

namespace ReconciliationApp.Application.Features.Imports;

public static class CsvSimpleParser
{
    // MVP: separa por \n y por coma. No soporta comillas/escapes aún.
    public static List<string> ParseToJsonRows(string csv)
    {
        csv = csv.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(csv)) return new();

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return new();

        var headers = lines[0]
            .Split(',')
            .Select(h => NormalizeHeader(h.Trim()))
            .ToArray();

        var result = new List<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',').Select(c => c.Trim()).ToArray();
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < headers.Length; c++)
            {
                dict[headers[c]] = c < cols.Length ? cols[c] : null;
            }

            result.Add(JsonSerializer.Serialize(dict));
        }

        return result;
    }

    private static string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return header;

        var normalized = header
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_");

        return normalized switch
        {
            "customerid" => "customer_id",
            "customer_id" => "customer_id",
            "customer" => "customer_id",
            "client" => "customer_id",
            "cliente" => "customer_id",
            "amount" => "amount",
            "importe" => "amount",
            "monto" => "amount",
            _ => normalized
        };
    }
}