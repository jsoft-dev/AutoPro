using ClosedXML.Excel;

namespace AutoPro.Api.Services;

public static class InventoryExcelTemplateBuilder
{
    public const string FileName = "inventory-import-template.xlsx";

    public static byte[] Build()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Inventory");

        sheet.Cell(1, 1).Value = "Part Number";
        sheet.Cell(1, 2).Value = "Name";
        sheet.Cell(1, 3).Value = "Quantity";
        sheet.Cell(1, 4).Value = "Unit Price";
        sheet.Cell(1, 5).Value = "Notes";
        sheet.Cell(1, 6).Value = "Supplier";

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF4");

        sheet.Cell(2, 1).Value = "BRK-001";
        sheet.Cell(2, 2).Value = "Brake pad set (front)";
        sheet.Cell(2, 3).Value = 12;
        sheet.Cell(2, 4).Value = 4500;
        sheet.Cell(2, 5).Value = "Optional notes";
        sheet.Cell(2, 6).Value = "Example Auto Parts";

        sheet.Columns(1, 6).AdjustToContents();
        sheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
        sheet.Column(3).Style.NumberFormat.Format = "#,##0.###";

        var instructions = workbook.Worksheets.Add("Instructions");
        instructions.Cell(1, 1).Value = "How to import inventory";
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(3, 1).Value =
            "1. Fill in rows on the Inventory sheet (delete the sample row or overwrite it).";
        instructions.Cell(4, 1).Value = "2. Part Number and Name are required. Quantity and Unit Price default to 0 if blank.";
        instructions.Cell(5, 1).Value =
            "3. Supplier is optional. On import, new supplier names can be created automatically.";
        instructions.Cell(6, 1).Value =
            "4. Save as .xlsx and use Import Excel on the Shop inventory page in the admin app.";
        instructions.Column(1).Width = 72;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}