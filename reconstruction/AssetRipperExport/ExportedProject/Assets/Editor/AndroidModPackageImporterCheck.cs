using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AndroidModPackageImporterCheck
{
    private const string Valid = "{\"id\":\"test-mod\",\"api\":1,\"enabled\":false,\"hook\":\"move_speed_multiplier\"}";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static MemoryStream Zip(params string[] files)
    {
        var result = new MemoryStream();
        using (var zip = new ZipArchive(result, ZipArchiveMode.Create, true))
            for (int i = 0; i < files.Length; i += 2)
                using (var writer = new StreamWriter(zip.CreateEntry(files[i]).Open(), new UTF8Encoding(false))) writer.Write(files[i + 1]);
        result.Position = 0;
        return result;
    }
    private static void Reject(string root, params string[] files)
    {
        bool rejected = false;
        try { using (var zip = Zip(files)) AndroidModPackageImporter.Install(zip, root); }
        catch (Exception e) when (e is InvalidDataException || e is IOException) { rejected = true; }
        Require(rejected, "invalid package was accepted");
    }
    public static void Run()
    {
        string root = Path.Combine(Application.temporaryCachePath, "mod-import-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var zip = Zip("manifest.json", Valid, "main.lua", "return 1.25"))
                Require(AndroidModPackageImporter.Install(zip, root) == "test-mod", "valid root package");
            string installed = Path.Combine(root, "test-mod", "main.lua");
            Require(File.ReadAllText(installed) == "return 1.25", "installed script");
            Reject(root, "manifest.json", Valid, "main.lua", "return 4");
            Require(File.ReadAllText(installed) == "return 1.25", "existing mod unchanged");
            string wrappedRoot = root + "-wrapped";
            using (var zip = Zip("test-mod/manifest.json", Valid, "test-mod/main.lua", "return 1"))
                Require(AndroidModPackageImporter.Install(zip, wrappedRoot) == "test-mod", "wrapped package");
            string damageRoot = root + "-damage";
            using (var zip = Zip("manifest.json", Valid.Replace("move_speed_multiplier", "damage_multiplier"), "main.lua", "return 1.5"))
                Require(AndroidModPackageImporter.Install(zip, damageRoot) == "test-mod", "damage hook package");
            string multi = "{\"id\":\"test-mod\",\"api\":2,\"hooks\":[\"move_speed_multiplier\",\"damage_multiplier\"]}";
            using (var zip = Zip("manifest.json", multi, "main.lua", "return {1.25, 1.5}"))
                Require(AndroidModPackageImporter.Install(zip, root + "-multi") == "test-mod", "multi-hook package");
            string invalidRoot = root + "-invalid";
            Reject(invalidRoot, "manifest.json", multi.Replace("damage_multiplier", "move_speed_multiplier"), "main.lua", "return {1, 1}");
            Reject(invalidRoot, "manifest.json", multi.Replace("damage_multiplier", "bad_hook"), "main.lua", "return {1, 1}");
            Reject(invalidRoot, "manifest.json", "{\"id\":\"test-mod\",\"api\":2,\"hooks\":[]}", "main.lua", "return {}");
            Reject(root, "../manifest.json", Valid, "main.lua", "return 1");
            Reject(root, "manifest.json", Valid.Replace("move_speed_multiplier", "unknown_hook"), "main.lua", "return 1");
            Reject(root, "other/manifest.json", Valid, "other/main.lua", "return 1");
            Reject(root, "manifest.json", Valid, "main.lua", "return 1", "main.lua", "return 2");
            Reject(root, "manifest.json", Valid, "main.lua", new string('x', 65537));
            Reject(root, "manifest.json", Valid);
            Require(Directory.GetDirectories(root).Length == 1, "no leftover temporary directories");
            Debug.Log("AndroidModPackageImporterCheck passed: root and wrapped ZIPs, existing package preservation, traversal, IDs, duplicates, size and missing files");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            if (Directory.Exists(root + "-wrapped")) Directory.Delete(root + "-wrapped", true);
            if (Directory.Exists(root + "-damage")) Directory.Delete(root + "-damage", true);
            if (Directory.Exists(root + "-multi")) Directory.Delete(root + "-multi", true);
            if (Directory.Exists(root + "-invalid")) Directory.Delete(root + "-invalid", true);
        }
    }
}
