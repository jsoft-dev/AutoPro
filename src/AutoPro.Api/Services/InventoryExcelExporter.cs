using ClosedXML.Excel;

namespace AutoPro.Api.Services;

public sealed record InventoryExcelExportRow(
    string PartNumber,
    string Name,
    decimal QuantityOnHand,
    decimal UnitPrice,
    string? Notes,
    string? SupplierName,
    bool IsActive);

public static class InventoryExcelExporter
{
    public static byte[] Build(IReadOnlyList<InventoryExcelExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inventory");

        sheet.Cell(1, 1).Value = "Part Number";
        sheet.Cell(1, 2).Value = "Name";
        sheet.Cell(1, 3).Value = "Quantity";
        sheet.Cell(1, 4).Value = "Unit Price";
        sheet.Cell(1, 5).Value = "Notes";
        sheet.Cell(1, 6).Value = "Supplier";
        sheet.Cell(1, 7).Value = "Active";

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF4");

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var r = i + 2;
            sheet.Cell(r, 1).Value = row.PartNumber;
            sheet.Cell(r, 2).Value = row.Name;
            sheet.Cell(r, 3).Value = row.QuantityOnHand;
            sheet.Cell(r, 4).Value = row.UnitPrice;
            sheet.Cell(r, 5).Value = row.Notes ?? string.Empty;
            sheet.Cell(r, 6).Value = row.SupplierName ?? string.Empty;
            sheet.Cell(r, 7).Value = row.IsActive ? "Yes" : "No";
        }

        sheet.Columns(1, 7).AdjustToContents();
        sheet.Column(3).Style.NumberFormat.Format = "#,##0.###";
        sheet.Column(4).Style.NumberFormat.Format = "#,##0.00";

        if (rows.Count > 0)
        {
            sheet.Range(1, 1, rows.Count + 1, 7).SetAutoFilter();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}