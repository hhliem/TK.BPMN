using System.IO.Compression;
using System.Security;
using System.Text;
using EnterpriseBpm.Analytics.Application.DTOs;

namespace EnterpriseBpm.Analytics.Infrastructure.Exporters;

/// <summary>
/// Bộ xuất file Excel XLSX OpenXML chuẩn hóa cao cho Big Data (TSK-708)
/// Không phụ thuộc thư viện ngoài, tối ưu hóa bộ nhớ và tốc độ xử lý trực tiếp qua ZipArchive Stream.
/// </summary>
public static class FastExcelExporter
{
    public static byte[] GenerateExcel(
        string title,
        IReadOnlyList<DynamicReportColumnDto> columns,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // 1. [Content_Types].xml
            WriteContentTypes(archive);

            // 2. _rels/.rels
            WriteRootRels(archive);

            // 3. xl/workbook.xml & xl/_rels/workbook.xml.rels
            WriteWorkbook(archive, title);

            // 4. xl/styles.xml (Định dạng màu sắc, font chữ, kẻ bảng, định dạng tiền tệ)
            WriteStyles(archive);

            // 5. xl/worksheets/sheet1.xml (Dữ liệu bảng tính thực tế)
            WriteSheetData(archive, title, columns, rows);
        }

        return memoryStream.ToArray();
    }

    public static byte[] GenerateCsv(
        IReadOnlyList<DynamicReportColumnDto> columns,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM để Excel hiển thị tiếng Việt có dấu chuẩn xác
        sb.Append('\uFEFF');

        // Headers
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Header))));

        // Data Rows
        foreach (var row in rows)
        {
            var values = columns.Select(c =>
            {
                if (row.TryGetValue(c.Field, out var val) && val != null)
                {
                    return EscapeCsv(val.ToString() ?? string.Empty);
                }
                return string.Empty;
            });
            sb.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string val)
    {
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
        return val;
    }

    private static void WriteContentTypes(ZipArchive archive)
    {
        var entry = archive.CreateEntry("[Content_Types].xml");
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>");
    }

    private static void WriteRootRels(ZipArchive archive)
    {
        var entry = archive.CreateEntry("_rels/.rels");
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
    }

    private static void WriteWorkbook(ZipArchive archive, string sheetName)
    {
        var safeName = SecurityElement.Escape(sheetName.Length > 30 ? sheetName.Substring(0, 30) : sheetName);

        var wbEntry = archive.CreateEntry("xl/workbook.xml");
        using (var writer = new StreamWriter(wbEntry.Open(), Encoding.UTF8))
        {
            writer.Write($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""{safeName}"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");
        }

        var relsEntry = archive.CreateEntry("xl/_rels/workbook.xml.rels");
        using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
        {
            writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>");
        }
    }

    private static void WriteStyles(ZipArchive archive)
    {
        var entry = archive.CreateEntry("xl/styles.xml");
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""2"">
    <font><name val=""Segoe UI""/><sz val=""10""/></font>
    <font><b/><color rgb=""FFFFFFFF""/><name val=""Segoe UI""/><sz val=""11""/></font>
  </fonts>
  <fills count=""3"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF4B549B""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/></border>
    <border>
      <left style=""thin""><color rgb=""FFD9D9D9""/></left>
      <right style=""thin""><color rgb=""FFD9D9D9""/></right>
      <top style=""thin""><color rgb=""FFD9D9D9""/></top>
      <bottom style=""thin""><color rgb=""FFD9D9D9""/></bottom>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""3"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""3"" fontId=""0"" fillId=""0"" borderId=""1"" applyNumberFormat=""1"" applyBorder=""1""/>
  </cellXfs>
</styleSheet>");
    }

    private static void WriteSheetData(
        ZipArchive archive,
        string reportTitle,
        IReadOnlyList<DynamicReportColumnDto> columns,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml");
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);

        writer.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
        writer.WriteLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

        // Column widths
        writer.WriteLine(@"  <cols>");
        for (int i = 0; i < columns.Count; i++)
        {
            var width = Math.Max(15, Math.Min(40, columns[i].Header.Length * 2));
            writer.WriteLine($@"    <col min=""{i + 1}"" max=""{i + 1}"" width=""{width}"" customWidth=""1""/>");
        }
        writer.WriteLine(@"  </cols>");

        writer.WriteLine(@"  <sheetData>");

        int rowIndex = 1;

        // Row 1: Header Titles (style index 1 = bold white on purple)
        writer.WriteLine($@"    <row r=""{rowIndex}"" ht=""28"" customHeight=""1"">");
        for (int colIndex = 0; colIndex < columns.Count; colIndex++)
        {
            var cellRef = GetCellReference(colIndex, rowIndex);
            var safeVal = SecurityElement.Escape(columns[colIndex].Header);
            writer.WriteLine($@"      <c r=""{cellRef}"" s=""1"" t=""inlineStr""><is><t>{safeVal}</t></is></c>");
        }
        writer.WriteLine(@"    </row>");
        rowIndex++;

        // Data Rows
        foreach (var row in rows)
        {
            writer.WriteLine($@"    <row r=""{rowIndex}"" ht=""20"" customHeight=""1"">");
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var col = columns[colIndex];
                var cellRef = GetCellReference(colIndex, rowIndex);

                if (row.TryGetValue(col.Field, out var rawVal) && rawVal != null)
                {
                    if (col.DataType is "number" or "currency" && decimal.TryParse(rawVal.ToString(), out var numVal))
                    {
                        var styleId = col.DataType == "currency" ? "2" : "0";
                        writer.WriteLine($@"      <c r=""{cellRef}"" s=""{styleId}""><v>{numVal}</v></c>");
                    }
                    else if (col.DataType == "date" && DateTime.TryParse(rawVal.ToString(), out var dtVal))
                    {
                        var formattedDate = SecurityElement.Escape(dtVal.ToString("dd/MM/yyyy HH:mm"));
                        writer.WriteLine($@"      <c r=""{cellRef}"" s=""0"" t=""inlineStr""><is><t>{formattedDate}</t></is></c>");
                    }
                    else
                    {
                        var safeVal = SecurityElement.Escape(rawVal.ToString() ?? string.Empty);
                        writer.WriteLine($@"      <c r=""{cellRef}"" s=""0"" t=""inlineStr""><is><t>{safeVal}</t></is></c>");
                    }
                }
                else
                {
                    writer.WriteLine($@"      <c r=""{cellRef}"" s=""0""/>");
                }
            }
            writer.WriteLine(@"    </row>");
            rowIndex++;
        }

        writer.WriteLine(@"  </sheetData>");
        writer.WriteLine(@"</worksheet>");
    }

    private static string GetCellReference(int columnIndex, int rowIndex)
    {
        var colName = string.Empty;
        var dividend = columnIndex + 1;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            colName = Convert.ToChar(65 + modulo) + colName;
            dividend = (dividend - modulo) / 26;
        }

        return $"{colName}{rowIndex}";
    }
}

