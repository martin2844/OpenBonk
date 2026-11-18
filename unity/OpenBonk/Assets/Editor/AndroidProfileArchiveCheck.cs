#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    private static void CheckVersionCompatibility(string root)
    {
        // Model an older export without later optional fields, plus fields a
        // different release might have added at the root or inside records.
        const string oldProgress = "{\"silver\":432,\"achievements\":[\"compatibility-check\"],\"future\":{\"mode\":2},\"menuMeta\":{\"mapsProgress\":{\"Forest\":{\"completedTiers\":[0],\"futureTier\":1}}}}";
        const string oldStats = "{\"stats\":{\"runs\":{\"name\":\"runs\",\"value\":9,\"futureTotal\":10}},\"future\":[true,12]}";
        using var zip = Zip("progression.json", SimpleEncryptor.Encrypt(oldProgress), "stats.json", SimpleEncryptor.Encrypt(oldStats));
        AndroidProfileArchive.Import(zip, root, "2");
        string account = Path.Combine(root, "2");
        var progress = JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(
            File.ReadAllText(Path.Combine(account, "progression.json"))));
        var stats = JsonConvert.DeserializeObject<StatsSaveFile>(SimpleEncryptor.Decrypt(
            File.ReadAllText(Path.Combine(account, "stats.json"))));
        Require(progress.silver == 432 && progress.achievements.Contains("compatibility-check")
            && progress.shopItems != null && stats.stats["runs"].value == 9 && stats.times != null,
            "An older profile did not retain its progress or defaults");
        progress.silver++;
        JObject savedProgress = JObject.FromObject(progress);
        JObject savedStats = JObject.FromObject(stats);
        Require((int)savedProgress["silver"] == 433 && (int)savedProgress["future"]["mode"] == 2
            && (int)savedProgress["menuMeta"]["mapsProgress"]["Forest"]["futureTier"] == 1,
            "A progression write lost an additive field");
        Require((int)savedStats["stats"]["runs"]["futureTotal"] == 10 && (bool)savedStats["future"][0],
            "A stats write lost an additive field");
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
            CheckVersionCompatibility(root);
            string fixture = Environment.GetEnvironmentVariable("MEGABONK_PROFILE_FIXTURE_DIR");
            if (!string.IsNullOrEmpty(fixture))
            {
                using var zip = Zip("progression.json", File.ReadAllText(Path.Combine(fixture, "progression.json")), "stats.json", File.ReadAllText(Path.Combine(fixture, "stats.json")));
                AndroidProfileArchive.Import(zip, root, "1");
                Debug.Log("Original encrypted profile shape accepted");
            }
            Debug.Log("AndroidProfileArchiveCheck passed: legacy ZIP compatibility, additive fields, safe paths, pair matching and interrupted replacement recovery");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
#endif
