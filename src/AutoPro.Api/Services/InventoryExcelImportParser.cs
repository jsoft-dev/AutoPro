using ClosedXML.Excel;

namespace AutoPro.Api.Services;

public sealed record ParsedInventoryImportRow(
    int RowNumber,
    string PartNumber,
    string Name,
    decimal QuantityOnHand,
    decimal UnitPrice,
    string? Notes,
    string? SupplierName);

public static class InventoryExcelImportParser
{
    private static readonly string[] PartNumberHeaders = ["partnumber", "part number", "part#", "part #", "sku"];
    private static readonly string[] NameHeaders = ["name", "part name", "description", "item"];
    private static readonly string[] QuantityHeaders = ["quantity", "qty", "quantity on hand", "on hand", "stock"];
    private static readonly string[] PriceHeaders = ["unitprice", "unit price", "price", "cost"];
    private static readonly string[] NotesHeaders = ["notes", "note", "remarks"];
    private static readonly string[] SupplierHeaders = ["supplier", "supplier name", "vendor", "vendor name"];

    public static IReadOnlyList<ParsedInventoryImportRow> Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Workbook has no worksheets.");

        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
            return Array.Empty<ParsedInventoryImportRow>();

        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastCol == 0)
            return Array.Empty<ParsedInventoryImportRow>();

        var columnMap = BuildColumnMap(headerRow, lastCol);
        if (!columnMap.ContainsKey("partNumber") || !columnMap.ContainsKey("name"))
            throw new InvalidOperationException(
                "Spreadsheet must include Part Number and Name columns (see import help).");

        var rows = new List<ParsedInventoryImportRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

        for (var rowNum = headerRow.RowNumber() + 1; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            var partNumber = GetCellText(row, columnMap, "partNumber");
            var name = GetCellText(row, columnMap, "name");
            if (string.IsNullOrWhiteSpace(partNumber) && string.IsNullOrWhiteSpace(name))
                continue;

            var quantity = GetDecimal(row, columnMap, "quantity") ?? 0m;
            var price = GetDecimal(row, columnMap, "price") ?? 0m;
            var notes = GetCellText(row, columnMap, "notes");
            var supplier = GetCellText(row, columnMap, "supplier");

            rows.Add(new ParsedInventoryImportRow(
                rowNum,
                partNumber.Trim(),
                name.Trim(),
                quantity,
                price,
                string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                string.IsNullOrWhiteSpace(supplier) ? null : supplier.Trim()));
        }

        return rows;
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow, int lastCol)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(header)) continue;

            if (PartNumberHeaders.Contains(header) && !map.ContainsKey("partNumber"))
                map["partNumber"] = col;
            else if (NameHeaders.Contains(header) && !map.ContainsKey("name"))
                map["name"] = col;
            else if (QuantityHeaders.Contains(header) && !map.ContainsKey("quantity"))
                map["quantity"] = col;
            else if (PriceHeaders.Contains(header) && !map.ContainsKey("price"))
                map["price"] = col;
            else if (NotesHeaders.Contains(header) && !map.ContainsKey("notes"))
                map["notes"] = col;
            else if (SupplierHeaders.Contains(header) && !map.ContainsKey("supplier"))
                map["supplier"] = col;
        }

        return map;
    }

    private static string GetCellText(IXLRow row, Dictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : string.Empty;

    private static decimal? GetDecimal(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var cell = row.Cell(col);
        if (cell.TryGetValue(out decimal d)) return d;
        if (cell.TryGetValue(out double dbl)) return (decimal)dbl;
        var text = cell.GetString().Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return decimal.TryParse(text, out var parsed) ? parsed : null;
    }
}