using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;

public static class XlsxParser
{
    public static bool DebugMode = false;

    public static List<GiftInfo> Parse(string path)
    {
        var result = new List<GiftInfo>();

        using var fs  = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

        if (DebugMode)
        {
            Debug.Log("[XlsxParser] Entries in zip:");
            foreach (var e in zip.Entries) Debug.Log($"  {e.FullName}");
        }

        // Shared strings (case-insensitive lookup)
        var strings = new List<string>();
        var ssEntry = GetEntry(zip, "xl/sharedStrings.xml");
        if (ssEntry != null)
        {
            using var ss = ssEntry.Open();
            var doc = XDocument.Load(ss);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (var si in doc.Descendants(ns + "si"))
                strings.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));

            if (DebugMode) Debug.Log($"[XlsxParser] Shared strings: {strings.Count}. First 5: {string.Join(", ", strings.Take(5))}");
        }
        else
        {
            Debug.LogWarning("[XlsxParser] sharedStrings.xml not found!");
        }

        // Sheet
        var sheetEntry = GetEntry(zip, "xl/worksheets/sheet1.xml");
        if (sheetEntry == null) { Debug.LogWarning("[XlsxParser] sheet1.xml not found!"); return result; }

        using var sheetStream = sheetEntry.Open();
        var sheet = XDocument.Load(sheetStream);
        XNamespace sns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        bool firstRow = true;
        foreach (var row in sheet.Descendants(sns + "row"))
        {
            if (firstRow) { firstRow = false; continue; }

            var cells = new Dictionary<int, string>();
            foreach (var cell in row.Elements(sns + "c"))
            {
                string cellRef = cell.Attribute("r")?.Value ?? "";
                int    col     = ColIndex(cellRef);
                string type    = cell.Attribute("t")?.Value ?? "";
                string rawVal  = cell.Element(sns + "v")?.Value ?? "";

                string val;
                switch (type)
                {
                    case "s":          // shared string
                        val = int.TryParse(rawVal, out int si) && si < strings.Count
                            ? strings[si] : rawVal;
                        break;
                    case "inlineStr":  // inline string
                        val = cell.Descendants(sns + "t").FirstOrDefault()?.Value ?? rawVal;
                        break;
                    case "str":        // formula result string
                    case "b":          // boolean
                    default:
                        val = rawVal;
                        break;
                }

                cells[col] = val;
            }

            if (DebugMode && result.Count < 3)
                Debug.Log($"[XlsxParser] Row cells: {string.Join(" | ", cells.Select(kv => $"[{kv.Key}]={kv.Value}"))}");

            if (!cells.TryGetValue(0, out string idStr)) continue;
            if (!int.TryParse(idStr.Split('.')[0], out int id)) continue;

            cells.TryGetValue(1, out string name);
            cells.TryGetValue(2, out string diamondStr);
            cells.TryGetValue(4, out string imageUrl);

            float.TryParse(diamondStr?.Split('.')[0] ?? "0",
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out float df);

            result.Add(new GiftInfo
            {
                id       = id,
                name     = name     ?? "",
                diamond  = (int)df,
                imageUrl = imageUrl ?? ""
            });
        }

        return result;
    }

    // Case-insensitive entry lookup
    private static ZipArchiveEntry GetEntry(ZipArchive zip, string name)
    {
        foreach (var entry in zip.Entries)
            if (string.Equals(entry.FullName, name, System.StringComparison.OrdinalIgnoreCase))
                return entry;
        return null;
    }

    private static int ColIndex(string cellRef)
    {
        int col = 0;
        foreach (char c in cellRef)
        {
            if (c < 'A' || c > 'Z') break;
            col = col * 26 + (c - 'A' + 1);
        }
        return col - 1;
    }
}
