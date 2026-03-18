using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace ReconciliationApp.Application.Features.Imports;

public static class CsvSimpleParser
{
    /// <summary>
    /// Parsea un CSV a una lista de JSON strings, uno por fila.
    /// Soporta campos con comas, comillas y saltos de línea dentro de celdas.
    /// </summary>
    public static List<string> ParseToJsonRows(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,       // no lanza error si faltan columnas
            BadDataFound = null,            // ignora datos malformados sin tirar excepción
        };

        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, config);

        var result = new List<string>();

        // Leemos los registros como diccionarios dinámicos
        csvReader.Read();
        csvReader.ReadHeader();
        var headers = csvReader.HeaderRecord ?? Array.Empty<string>();

        while (csvReader.Read())
        {
            var dict = new Dictionary<string, string?>();

            foreach (var header in headers)
            {
                dict[header] = csvReader.GetField(header);
            }

            result.Add(JsonSerializer.Serialize(dict));
        }

        return result;
    }
}
