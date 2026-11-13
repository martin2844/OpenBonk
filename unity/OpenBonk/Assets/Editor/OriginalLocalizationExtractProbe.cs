#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class OriginalLocalizationExtractProbe
{
    [Serializable] private sealed class Export { public int version = 1; public string sourceBundle; public List<Table> tables = new List<Table>(); }
    [Serializable] private sealed class Table { public string asset; public List<Entry> entries = new List<Entry>(); }
    [Serializable] private sealed class Entry { public string id; public string value; }
    public static void Run()
    {
        string data = Environment.GetEnvironmentVariable("MEGABONK_ORIGINAL_DATA") ?? "/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data";
        string zipPath = Environment.GetEnvironmentVariable("MEGABONK_GAME_ZIP");
        string bundleName = Environment.GetEnvironmentVariable("MEGABONK_LOCALE_BUNDLE") ?? "localization-string-tables-english(en)_assets_all.bundle";
        if (!bundleName.StartsWith("localization-string-tables-", StringComparison.Ordinal) || !bundleName.EndsWith("_assets_all.bundle", StringComparison.Ordinal) || Path.GetFileName(bundleName) != bundleName || bundleName.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            throw new Exception("Invalid localization bundle name");
        string output = Environment.GetEnvironmentVariable("MEGABONK_LOCALIZATION_OUTPUT");
        if (!string.IsNullOrEmpty(output) && Path.GetFullPath(output).StartsWith(Path.GetFullPath(data) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new Exception("Output must be outside the original game data directory");
        if (!string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(zipPath) && Path.GetFullPath(output) == Path.GetFullPath(zipPath))
            throw new Exception("Output must not overwrite the input ZIP");
        var export = new Export { sourceBundle = bundleName };
        string temporary = null;
        AssetBundle bundle = null;
        try
        {
            string file;
            if (string.IsNullOrEmpty(zipPath)) file = Path.Combine(data, "StreamingAssets/aa/StandaloneLinux64", bundleName);
            else { temporary = ExtractBundle(zipPath, bundleName); file = temporary; }
            bundle = AssetBundle.LoadFromFile(file);
            if (bundle == null) throw new Exception("Original Linux bundle could not be opened in the Linux editor");
            var names = bundle.GetAllAssetNames(); Array.Sort(names, StringComparer.Ordinal);
            foreach (string name in names)
            {
                var asset = bundle.LoadAsset<UnityEngine.Object>(name);
                if (asset is StringTable table)
                {
                    var values = new List<string>();
                    var tableExport = new Table { asset = name };
                    foreach (var entry in table.Values)
                    {
                        values.Add(entry.KeyId + "\t" + entry.LocalizedValue);
                        tableExport.entries.Add(new Entry { id = entry.KeyId.ToString(System.Globalization.CultureInfo.InvariantCulture), value = entry.LocalizedValue });
                    }
                    export.tables.Add(tableExport);
                    values.Sort(StringComparer.Ordinal);
                    using var hash = SHA256.Create();
                    string digest = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", values)))).Replace("-", "").ToLowerInvariant();
                    Debug.Log($"Original PC bundle table={name} entries={values.Count} contentSha256={digest}");
                }
                else Debug.Log($"Original PC bundle asset={name} type={(asset == null ? "null" : asset.GetType().FullName)}");
            }
            if (!string.IsNullOrEmpty(output))
            {
                File.WriteAllText(output, JsonUtility.ToJson(export, true), new UTF8Encoding(false));
                Debug.Log($"Original PC bundle exported {export.tables.Count} tables to {output}");
            }
            Debug.Log($"Original PC bundle assets={names.Length} DONE");
        }
        finally { if (bundle != null) bundle.Unload(true); if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
    }

    private static string ExtractBundle(string zipPath, string bundleName)
    {
        const long limit = 8 * 1024 * 1024;
        string expected = "StreamingAssets/aa/StandaloneLinux64/" + bundleName;
        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count > 20000) throw new InvalidDataException("Too many ZIP entries");
        ZipArchiveEntry match = null;
        foreach (var entry in archive.Entries)
        {
            string name = entry.FullName;
            if (name.StartsWith("/", StringComparison.Ordinal) || name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0) throw new InvalidDataException("Unsafe ZIP path");
            foreach (string part in name.Split('/')) if (part == "." || part == "..") throw new InvalidDataException("Unsafe ZIP path");
            if (name != expected && !name.EndsWith("/Megabonk_Data/" + expected, StringComparison.Ordinal) && name != "Megabonk_Data/" + expected) continue;
            if (match != null) throw new InvalidDataException("Multiple matching localization bundles");
            match = entry;
        }
        if (match == null || match.Length <= 0 || match.Length > limit) throw new InvalidDataException("Missing or oversized localization bundle");
        string temp = Path.GetTempFileName();
        try
        {
            using var input = match.Open();
            using var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[32768]; long total = 0; int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) != 0) { total += read; if (total > limit) throw new InvalidDataException("Localization bundle too large"); output.Write(buffer, 0, read); }
            if (total != match.Length) throw new InvalidDataException("Incomplete localization bundle");
            return temp;
        }
        catch { File.Delete(temp); throw; }
    }
}
#endif
