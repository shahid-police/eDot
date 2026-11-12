using System.Text;

namespace Elegance.Infrastructure.Marketing;

// Deliberately plain CSV, no library - every report here is a flat list of simple scalar columns,
// which is all a spreadsheet needs. UTF-8 with a BOM so Excel (not just Sheets) reads it correctly.
public static class CsvExporter
{
    public static byte[] Write<T>(IEnumerable<T> rows, params (string Header, Func<T, object?> Selector)[] columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns.Select(c => Escape(c.Header))));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', columns.Select(c => Escape(c.Selector(row)?.ToString() ?? ""))));
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
