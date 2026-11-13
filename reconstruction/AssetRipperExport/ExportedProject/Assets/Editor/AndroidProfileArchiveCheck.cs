#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

public static class AndroidProfileArchiveCheck
{
    private static MemoryStream Zip(params string[] pairs)
    {
        var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
            for (int i = 0; i < pairs.Length; i += 2)
                using (var writer = new StreamWriter(zip.CreateEntry(pairs[i]).Open(), new UTF8Encoding(false))) writer.Write(pairs[i + 1]);
        output.Position = 0; return output;
    }
    private static void Require(bool value, string text) { if (!value) throw new Exception(text); }
    private static void Reject(string root, params string[] pairs)
    {
        bool failed = false;
        try { using var zip = Zip(pairs); AndroidProfileArchive.Import(zip, root, "0"); }
        catch (InvalidDataException) { failed = true; }
        Require(failed, "Invalid profile accepted");
    }
    public static void Run()
    {
        string root = Path.Combine(Application.temporaryCachePath, "profile-check-" + Guid.NewGuid().ToString("N"));
        string current = Path.Combine(root, "0"), previous = Path.Combine(root, ".profile-previous-0");
        string progression = SimpleEncryptor.Encrypt(JsonConvert.SerializeObject(new ProgressionSaveFile { silver = 123 }));
        string stats = SimpleEncryptor.Encrypt(JsonConvert.SerializeObject(new StatsSaveFile()));
        try
        {
            Directory.CreateDirectory(current);
            File.WriteAllText(Path.Combine(current, "progression.json"), "old progression");
            File.WriteAllText(Path.Combine(current, "stats.json"), "old stats");
            File.WriteAllText(Path.Combine(current, "other"), "keep");
            Reject(root, "progression.json", progression, "stats.json", "bad");
            Reject(root, "../progression.json", progression, "stats.json", stats);
            Reject(root, "one/progression.json", progression, "two/stats.json", stats);
            Reject(root, "progression.json", progression, "stats.json", stats, "progression.json", progression);
            Require(File.ReadAllText(Path.Combine(current, "progression.json")) == "old progression", "failed import changed profile");
            using (var zip = Zip("Steam/0/progression.json", progression, "Steam/0/stats.json", stats))
            {
                string backup = AndroidProfileArchive.Import(zip, root, "0");
                Require(backup != null && File.ReadAllText(Path.Combine(backup, "progression.json")) == "old progression", "missing preserved backup");
                Require(File.ReadAllText(Path.Combine(current, "progression.json")) == progression && File.ReadAllText(Path.Combine(current, "stats.json")) == stats, "import contents");
                Require(File.ReadAllText(Path.Combine(current, "other")) == "keep", "unrelated account file changed");
            }
            Directory.Move(current, previous); AndroidProfileArchive.Recover(root, "0");
            Require(Directory.Exists(current) && !Directory.Exists(previous), "interrupted replacement did not restore");
            Directory.CreateDirectory(previous); File.WriteAllText(Path.Combine(previous, "old"), "preserve");
            int before = Directory.GetDirectories(root, ".profile-backup-0-*").Length;
            AndroidProfileArchive.Recover(root, "0");
            Require(Directory.Exists(current) && !Directory.Exists(previous) && Directory.GetDirectories(root, ".profile-backup-0-*").Length == before + 1, "committed replacement recovery");
            string fixture = Environment.GetEnvironmentVariable("MEGABONK_PROFILE_FIXTURE_DIR");
            if (!string.IsNullOrEmpty(fixture))
            {
                using var zip = Zip("progression.json", File.ReadAllText(Path.Combine(fixture, "progression.json")), "stats.json", File.ReadAllText(Path.Combine(fixture, "stats.json")));
                AndroidProfileArchive.Import(zip, root, "1");
                Debug.Log("Original encrypted profile shape accepted");
            }
            Debug.Log("AndroidProfileArchiveCheck passed: profile validation, safe paths, pair matching, preservation and interrupted replacement recovery");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
#endif
