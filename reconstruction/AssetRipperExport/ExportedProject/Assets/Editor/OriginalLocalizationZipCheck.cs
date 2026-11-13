#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UnityEngine;

public static class OriginalLocalizationZipCheck
{
    public static void Run()
    {
        string dir = Path.Combine(Path.GetTempPath(), "megabonk-zip-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        const string name = "localization-string-tables-spanish(es)_assets_all.bundle";
        const string path = "Megabonk/Megabonk_Data/StreamingAssets/aa/StandaloneLinux64/" + name;
        MethodInfo extract = typeof(OriginalLocalizationExtractProbe).GetMethod("ExtractBundle", BindingFlags.NonPublic | BindingFlags.Static);
        try
        {
            string good = MakeZip(dir, "valid.zip", new[] { (path, "test bundle"), ("Megabonk/other", "other") });
            string temporary = (string)extract.Invoke(null, new object[] { good, name });
            try { if (File.ReadAllText(temporary) != "test bundle") throw new Exception("wrong extracted bytes"); }
            finally { File.Delete(temporary); }
            string duplicate = MakeZip(dir, "duplicate.zip", new[] { (path, "first"), (path, "second") });
            string traversal = MakeZip(dir, "traversal.zip", new[] { (path, "test"), ("../evil", "bad") });
            string missing = MakeZip(dir, "missing.zip", new[] { ("unrelated", "test") });
            foreach (string invalid in new[] { duplicate, traversal, missing })
            {
                bool rejected = false;
                try { string file = (string)extract.Invoke(null, new object[] { invalid, name }); File.Delete(file); }
                catch (TargetInvocationException e) when (e.InnerException is InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception("ZIP should have been rejected: " + invalid);
            }
            Debug.Log("Original localization ZIP checks PASS: wrapped, duplicate, traversal, missing");
        }
        finally { Directory.Delete(dir, true); }
    }
    private static string MakeZip(string dir, string file, (string, string)[] contents)
    {
        string path = Path.Combine(dir, file);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var item in contents) { var entry = zip.CreateEntry(item.Item1); using var writer = new StreamWriter(entry.Open()); writer.Write(item.Item2); }
        return path;
    }
}
#endif
