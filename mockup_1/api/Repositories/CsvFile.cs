using System.Text;

namespace api.Repositories;

// Minimal CSV reader/writer. Values containing commas, quotes or line breaks are wrapped
// in quotes, so captions and descriptions can contain any text.
public static class CsvFile
{
    // Creates the file if missing. If the file has an older header (fewer columns),
    // rewrites it with the new header and fills the new columns with "".
    public static void EnsureSchema(string path, string header)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, header + "\n");
            return;
        }

        var rows = Parse(File.ReadAllText(path));
        if (rows.Count > 0 && string.Join(",", rows[0]) == header)
            return;

        var columnCount = header.Split(',').Length;
        var upgraded = rows.Skip(1)
            .Where(r => !IsBlank(r))
            .Select(r => r.Concat(Enumerable.Repeat("", Math.Max(0, columnCount - r.Length))).ToArray());
        WriteAll(path, header, upgraded);
    }

    // Returns every row except the header.
    public static List<string[]> ReadRows(string path)
    {
        return Parse(File.ReadAllText(path)).Skip(1).Where(r => !IsBlank(r)).ToList();
    }

    public static void AppendRow(string path, IEnumerable<string> values)
    {
        File.AppendAllText(path, ToLine(values));
    }

    // Rewrites the whole file safely: write a temp file first, then swap it in,
    // so a crash mid-write can't leave a half-written file. No backup copy is kept,
    // so deleted rows are really gone.
    public static void WriteAll(string path, string header, IEnumerable<string[]> rows)
    {
        var temp = path + ".tmp";
        var text = new StringBuilder(header + "\n");
        foreach (var row in rows)
            text.Append(ToLine(row));
        File.WriteAllText(temp, text.ToString());

        if (File.Exists(path))
            File.Replace(temp, path, null);
        else
            File.Move(temp, path);
    }

    private static bool IsBlank(string[] row) => row.Length == 1 && row[0] == "";

    private static string ToLine(IEnumerable<string> values) =>
        string.Join(",", values.Select(Escape)) + "\n";

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else field.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\n')
            {
                row.Add(field.ToString()); field.Clear();
                rows.Add(row.ToArray()); row.Clear();
            }
            else if (c != '\r') field.Append(c);
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row.ToArray());
        }
        return rows;
    }
}
