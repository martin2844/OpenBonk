#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public static class AndroidOriginalLocalizationZipCheck
{
    public static void Run()
    {
        const string bundle = "localization-string-tables-spanish(es)_assets_all.bundle";
        const string shared = "localization-assets-shared_assets_all.bundle";
        string root = "/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data/StreamingAssets/aa/StandaloneLinux64";
        byte[] strings = File.ReadAllBytes(Path.Combine(root, bundle)), common = File.ReadAllBytes(Path.Combine(root, shared));
        using (var valid = Zip(strings, common, "valid"))
        {
            var result = AndroidOriginalLocalizationZip.Convert(valid, bundle);
            if (!result.Strings.SequenceEqual(AndroidLocalizationBundleConverter.Convert(strings)) || !result.SharedData.SequenceEqual(AndroidLocalizationBundleConverter.Convert(common))) throw new Exception("ZIP output differs from direct conversion");
        }
        foreach (string variant in new[] { "duplicate", "traversal", "missing" })
        {
            using var invalid = Zip(strings, common, variant); bool rejected = false;
            try { AndroidOriginalLocalizationZip.Convert(invalid, bundle); } catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("ZIP accepted: " + variant);
        }
        Debug.Log("Android localization ZIP PASS: valid, duplicate, traversal, missing");
    }
    private static MemoryStream Zip(byte[] strings, byte[] shared, string variant)
    {
        var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            string root = "Megabonk/Megabonk_Data/StreamingAssets/aa/StandaloneLinux64/";
            Add(zip, root + "localization-string-tables-spanish(es)_assets_all.bundle", strings);
            if (variant != "missing") Add(zip, root + "localization-assets-shared_assets_all.bundle", shared);
            if (variant == "duplicate") Add(zip, root + "localization-assets-shared_assets_all.bundle", shared);
            if (variant == "traversal") Add(zip, "../unsafe", new byte[] { 1 });
        }
        memory.Position = 0; return memory;
    }
    private static void Add(ZipArchive zip, string name, byte[] bytes) { using var stream = zip.CreateEntry(name).Open(); stream.Write(bytes, 0, bytes.Length); }
}
#endif
