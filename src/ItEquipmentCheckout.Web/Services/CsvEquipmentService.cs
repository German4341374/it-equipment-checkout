using System.Globalization;
using System.Text;
using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Services;

public sealed class CsvEquipmentService(CheckoutDbContext dbContext)
{
    private static readonly string[] RequiredColumns =
    [
        "inventoryNumber",
        "category",
        "manufacturer",
        "model",
        "serialNumber",
        "expectedReturnDays",
    ];

    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken)
    {
        List<Equipment> equipment = await dbContext.Equipment
            .AsNoTracking()
            .Include(item => item.Checkouts.Where(checkout => checkout.ActualReturnDate == null))
                .ThenInclude(checkout => checkout.Employee)
            .OrderBy(item => item.InventoryNumber)
            .ToListAsync(cancellationToken);

        var output = new StringBuilder();
        AppendRow(
            output,
            "inventoryNumber",
            "category",
            "manufacturer",
            "model",
            "serialNumber",
            "status",
            "expectedReturnDays",
            "checkedOutTo",
            "plannedReturnDate");
        foreach (Equipment item in equipment)
        {
            Checkout? active = item.Checkouts.SingleOrDefault();
            AppendRow(
                output,
                item.InventoryNumber,
                item.Category.ToString(),
                item.Manufacturer,
                item.Model,
                item.SerialNumber,
                item.Status.ToString(),
                item.ExpectedReturnDays.ToString(CultureInfo.InvariantCulture),
                active?.Employee.FullName ?? string.Empty,
                active?.PlannedReturnDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(output.ToString());
    }

    public async Task<CsvImportResult> ImportAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        string? headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine is null)
        {
            return new CsvImportResult(0, 1, [new CsvImportError(1, "CSV file is empty.")]);
        }

        List<string> headers;
        try
        {
            headers = ParseRow(headerLine);
        }
        catch (FormatException exception)
        {
            return new CsvImportResult(0, 1, [new CsvImportError(1, exception.Message)]);
        }

        var positions = headers
            .Select((name, index) => new { Name = name.Trim(), Index = index })
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
        string[] missing = RequiredColumns.Where(column => !positions.ContainsKey(column)).ToArray();
        if (missing.Length > 0)
        {
            return new CsvImportResult(
                0,
                1,
                [new CsvImportError(1, $"Missing required columns: {string.Join(", ", missing)}.")]);
        }

        var existingInventory = await dbContext.Equipment
            .Select(item => item.InventoryNumber)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingSerials = await dbContext.Equipment
            .Select(item => item.SerialNumber)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
        var errors = new List<CsvImportError>();
        int imported = 0;
        int rowNumber = 1;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            rowNumber++;
            if (rowNumber > 1001)
            {
                errors.Add(new CsvImportError(rowNumber, "Import is limited to 1,000 data rows."));
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            List<string> values;
            try
            {
                values = ParseRow(line);
            }
            catch (FormatException exception)
            {
                errors.Add(new CsvImportError(rowNumber, exception.Message));
                continue;
            }

            string Value(string column)
            {
                int index = positions[column];
                return index < values.Count ? values[index].Trim() : string.Empty;
            }

            string inventoryNumber = Value("inventoryNumber");
            string manufacturer = Value("manufacturer");
            string model = Value("model");
            string serialNumber = Value("serialNumber");
            if (inventoryNumber.Length is < 2 or > 40 ||
                manufacturer.Length is < 1 or > 80 ||
                model.Length is < 1 or > 100 ||
                serialNumber.Length is < 2 or > 100)
            {
                errors.Add(new CsvImportError(rowNumber, "One or more required text fields have invalid lengths."));
                continue;
            }

            if (!Enum.TryParse(Value("category"), ignoreCase: true, out EquipmentCategory category))
            {
                errors.Add(new CsvImportError(rowNumber, "Category is not supported."));
                continue;
            }

            if (!int.TryParse(
                    Value("expectedReturnDays"),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int expectedReturnDays) ||
                expectedReturnDays is < 1 or > 365)
            {
                errors.Add(new CsvImportError(rowNumber, "expectedReturnDays must be between 1 and 365."));
                continue;
            }

            if (!existingInventory.Add(inventoryNumber))
            {
                errors.Add(new CsvImportError(rowNumber, "inventoryNumber already exists in the file or database."));
                continue;
            }

            if (!existingSerials.Add(serialNumber))
            {
                existingInventory.Remove(inventoryNumber);
                errors.Add(new CsvImportError(rowNumber, "serialNumber already exists in the file or database."));
                continue;
            }

            var item = new Equipment
            {
                InventoryNumber = inventoryNumber,
                Category = category,
                Manufacturer = manufacturer,
                Model = model,
                SerialNumber = serialNumber,
                ExpectedReturnDays = expectedReturnDays,
            };
            item.Events.Add(new EquipmentEvent
            {
                EquipmentId = item.Id,
                Type = EquipmentEventType.Imported,
                Description = $"Imported from CSV row {rowNumber}.",
            });
            dbContext.Equipment.Add(item);
            imported++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CsvImportResult(imported, errors.Count, errors);
    }

    private static void AppendRow(StringBuilder output, params string[] values)
    {
        output.AppendJoin(',', values.Select(Escape));
        output.AppendLine();
    }

    private static string Escape(string value)
    {
        string safe = value.Length > 0 && "=+-@".Contains(value[0], StringComparison.Ordinal)
            ? $"'{value}"
            : value;
        return $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    internal static List<string> ParseRow(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    current.Append(character);
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else if (character == '"' && current.Length == 0)
            {
                quoted = true;
            }
            else
            {
                current.Append(character);
            }
        }

        if (quoted)
        {
            throw new FormatException("CSV row contains an unclosed quoted field.");
        }

        values.Add(current.ToString());
        return values;
    }
}
