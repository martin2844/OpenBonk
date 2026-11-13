#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

// Uses only a separate disposable directory, never the game's actual save path.
internal static class AndroidProfileArchiveProbe
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "megabonk_profile_archive_probe", false)) return;
        string root = Path.Combine(Application.persistentDataPath, "ProfileArchiveProbe-" + Guid.NewGuid().ToString("N"));
        string current = Path.Combine(root, "0"), pending = Path.Combine(root, ".profile-previous-0");
        try
        {
            Directory.CreateDirectory(current);
            File.WriteAllText(Path.Combine(current, "progression.json"), "previous");
            File.WriteAllText(Path.Combine(current, "controller_config.json"), "keep controller");
            string progression = SimpleEncryptor.Encrypt(JsonConvert.SerializeObject(new ProgressionSaveFile { silver = 123 }));
            string stats = SimpleEncryptor.Encrypt(JsonConvert.SerializeObject(new StatsSaveFile()));
            using var memory = new MemoryStream();
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
                foreach (var item in new[] { ("account/progression.json", progression), ("account/stats.json", stats) })
                    using (var writer = new StreamWriter(zip.CreateEntry(item.Item1).Open(), new UTF8Encoding(false))) writer.Write(item.Item2);
            memory.Position = 0;
            string backup = AndroidProfileArchive.Import(memory, root, "0");
            if (File.ReadAllText(Path.Combine(backup, "progression.json")) != "previous" ||
                File.ReadAllText(Path.Combine(current, "controller_config.json")) != "keep controller" ||
                File.ReadAllText(Path.Combine(current, "progression.json")) != progression ||
                File.ReadAllText(Path.Combine(current, "stats.json")) != stats) throw new Exception("Import did not preserve expected data");
            Directory.Move(current, pending); AndroidProfileArchive.Recover(root, "0");
            if (!Directory.Exists(current) || Directory.Exists(pending) || File.ReadAllText(Path.Combine(current, "stats.json")) != stats) throw new Exception("Recovery failed");
            Debug.Log("Android profile archive probe PASS: imported pair, preserved backup and controller file, recovered interrupted rename");
        }
        catch (Exception e) { Debug.LogError("Android profile archive probe FAILED: " + e); }
        finally { try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (Exception e) { Debug.LogWarning("Profile probe cleanup failed: " + e.Message); } }
    }
}
#endif
