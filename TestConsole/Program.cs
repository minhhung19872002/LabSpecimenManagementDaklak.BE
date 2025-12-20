using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var basePath = @"C:\Users\DanhPV\Desktop\Free\LSM\LabSpecimenManagementDaklak.BE\src\LabSpecimenManagement.HttpApi.Host\wwwroot";
var templatePath = Path.Combine(basePath, "templates", "Form trả kq BM03-QT13-BCKQ_3.2.template.docx");
var outputPath = Path.Combine(basePath, "temp", "test_output.docx");

Console.WriteLine($"Template: {templatePath}");
Console.WriteLine($"Output: {outputPath}");

// Copy template
File.Copy(templatePath, outputPath, true);
Console.WriteLine("Template copied.");

// Test data
var tableData = new List<(int No, string Parameters, string Unit, string TestMethod, string Result, string Standards)>
{
    (1, "Tổng vi khuẩn", "CFU/g", "TCVN123", "100", "QCVN 01"),
    (2, "Coliform", "CFU/g", "TCVN456", "50", "QCVN 02"),
    (3, "E.coli", "CFU/g", "TCVN789", "10", "QCVN 03"),
};

using (var document = WordprocessingDocument.Open(outputPath, true))
{
    var body = document.MainDocumentPart?.Document?.Body;
    if (body == null) { Console.WriteLine("Body is null"); return; }

    // Helper functions
    string GetDirectRowText(TableRow row)
    {
        var texts = new List<string>();
        foreach (var cell in row.Elements<TableCell>())
        {
            foreach (var para in cell.Elements<Paragraph>())
            {
                texts.Add(string.Concat(para.Descendants<Text>().Select(t => t.Text ?? "")));
            }
        }
        return string.Join("", texts);
    }

    var allTables = body.Descendants<Table>().ToList();
    Console.WriteLine($"Found {allTables.Count} tables in document");

    Table? targetTable = null;
    TableRow? templateRow = null;

    foreach (var table in allTables.AsEnumerable().Reverse())
    {
        var directRows = table.Elements<TableRow>().ToList();

        foreach (var row in directRows)
        {
            var hasNestedTable = row.Descendants<Table>().Any();
            var rowText = GetDirectRowText(row);

            if (rowText.Contains("{{No}}") || rowText.Contains("{{TestTable}}"))
            {
                Console.WriteLine($"Found row with placeholder. HasNestedTable: {hasNestedTable}");
                Console.WriteLine($"Row text: {rowText.Substring(0, Math.Min(100, rowText.Length))}...");

                if (!hasNestedTable)
                {
                    targetTable = table;
                    templateRow = row;
                    Console.WriteLine("This row is selected as template row!");
                    break;
                }
                else
                {
                    Console.WriteLine("Skipping this row because it has nested table.");
                }
            }
        }
        if (targetTable != null) break;
    }

    if (targetTable == null || templateRow == null)
    {
        Console.WriteLine("ERROR: Could not find target table or template row!");
        return;
    }

    Console.WriteLine("\n=== Processing table ===");

    var allRows = targetTable.Elements<TableRow>().ToList();
    var templateRowIndex = allRows.IndexOf(templateRow);
    Console.WriteLine($"Template row index: {templateRowIndex} (of {allRows.Count} total rows)");

    var templateClone = (TableRow)templateRow.CloneNode(true);
    templateRow.Remove();

    allRows = targetTable.Elements<TableRow>().ToList();
    Console.WriteLine($"Rows after removal: {allRows.Count}");

    TableRow? insertAfterRow = null;
    if (templateRowIndex > 0 && allRows.Count > 0)
    {
        insertAfterRow = allRows[templateRowIndex - 1];
        Console.WriteLine($"Will insert after row index {templateRowIndex - 1}");
    }

    foreach (var data in tableData)
    {
        var newRow = (TableRow)templateClone.CloneNode(true);

        foreach (var text in newRow.Descendants<Text>())
        {
            if (string.IsNullOrEmpty(text.Text)) continue;
            text.Text = text.Text
                .Replace("{{TestTable}}", "")
                .Replace("{{No}}", data.No.ToString())
                .Replace("{{Parameters}}", data.Parameters)
                .Replace("{{Unit}}", data.Unit)
                .Replace("{{TestMethod}}", data.TestMethod)
                .Replace("{{Result}}", data.Result)
                .Replace("{{Standards}}", data.Standards)
                .Replace("{{Uncertainty}}", "")
                .Replace("{{Proclaim}}", "");
        }

        if (insertAfterRow != null)
        {
            insertAfterRow.InsertAfterSelf(newRow);
            insertAfterRow = newRow;
        }
        else
        {
            var firstRow = targetTable.Elements<TableRow>().FirstOrDefault();
            if (firstRow != null) firstRow.InsertBeforeSelf(newRow);
            else targetTable.AppendChild(newRow);
            insertAfterRow = newRow;
        }
        Console.WriteLine($"Inserted row {data.No}");
    }

    document.MainDocumentPart?.Document?.Save();
}

Console.WriteLine("\n=== Verifying output ===");

// Verify output
using (var document = WordprocessingDocument.Open(outputPath, false))
{
    var body = document.MainDocumentPart?.Document?.Body;
    var tables = body!.Descendants<Table>().ToList();
    Console.WriteLine($"Output has {tables.Count} tables");

    int dataTableCount = 0;
    foreach (var table in tables)
    {
        var rows = table.Elements<TableRow>().ToList();
        var tableText = string.Concat(table.Descendants<Text>().Select(t => t.Text ?? ""));
        if (tableText.Contains("Chỉ tiêu") || tableText.Contains("Parameters"))
        {
            dataTableCount++;
            Console.WriteLine($"Data table #{dataTableCount} found with {rows.Count} rows");

            // Show first 5 rows
            for (int i = 0; i < Math.Min(5, rows.Count); i++)
            {
                var rowText = string.Concat(rows[i].Descendants<Text>().Select(t => t.Text ?? ""));
                Console.WriteLine($"  Row {i}: {rowText.Substring(0, Math.Min(60, rowText.Length))}...");
            }
        }
    }

    if (dataTableCount == 1)
    {
        Console.WriteLine("\n*** SUCCESS: Only 1 data table found! ***");
    }
    else
    {
        Console.WriteLine($"\n*** FAILED: Found {dataTableCount} data tables instead of 1 ***");
    }
}

Console.WriteLine("\nDone!");
