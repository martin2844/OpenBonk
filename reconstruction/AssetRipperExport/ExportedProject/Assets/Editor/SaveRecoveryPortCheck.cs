using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SaveRecoveryPortCheck
{
    static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SaveRecoveryPortCheck: " + message); }
    public static void Run()
    {
        var pathField = typeof(SaveManager).GetField("defaultSavesPath", PrivateStatic);
        var idField = typeof(SaveManager).GetField("steamIdSave", PrivateStatic);
        var testField = typeof(SaveManager).GetField("TEST_SAVES", PrivateStatic);
        string oldPath = (string)pathField.GetValue(null); ulong oldId = (ulong)idField.GetValue(null); bool oldTest = (bool)testField.GetValue(null); bool oldLoaded = SaveManager.loaded;
        string name = "codex-save-recovery-check-" + Guid.NewGuid().ToString("N");
        string dir = Path.Combine(Application.persistentDataPath, name);
        const string pref = "saves_last_steamid"; bool hadPref = PlayerPrefs.HasKey(pref); string oldPref = PlayerPrefs.GetString(pref, "");
        var root = new GameObject("save recovery check"); root.SetActive(false); var save = root.AddComponent<SaveManager>();
        try
        {
            pathField.SetValue(null, name); testField.SetValue(null, false); idField.SetValue(null, 69UL); SaveManager.loaded = false; PlayerPrefs.DeleteKey(pref);
            string reason, failPath;
            int initialGameOverListeners = GameManager.A_GameOver?.GetInvocationList().Length ?? 0;
            Check(save.Load(0, false, out reason, out failPath), "initial temporary load: " + reason);
            Check((GameManager.A_GameOver?.GetInvocationList().Length ?? 0) == initialGameOverListeners + 1, "initial progression listener");
            save.config.cfVideoSettings.fov = 80f; save.SaveConfig(); save.SaveStats(); save.SaveProgression();
            string config = Path.Combine(dir, "LocalDir", "config.json"); string stats = Path.Combine(dir, "CloudDir", "0", "stats.json"); string progress = Path.Combine(dir, "CloudDir", "0", "progression.json");
            Check(File.Exists(config) && File.Exists(stats) && File.Exists(progress), "all temporary files created");
            save.config.cfVideoSettings.fov = 90f; save.SaveConfig();
            string backup = File.ReadAllText(config + ".bak"); Check(backup.Contains("80.0") || backup.Contains("80"), "first config version in backup");
            File.WriteAllText(config, "{broken"); SaveManager.loaded = false;
            Check(save.Load(0, true, out reason, out failPath), "backup recovery with other backups missing: " + reason);
            Check((GameManager.A_GameOver?.GetInvocationList().Length ?? 0) == initialGameOverListeners + 1, "recovery replaces progression listener");
            Check(Mathf.Approximately(save.config.cfVideoSettings.fov, 80f), "backup value restored");
            save.SaveConfig(); Check(File.ReadAllText(config + ".bak") == backup, "first post-recovery save preserves good backup");
            save.stats.stats["runs"].value = 12; save.progression.silver = 123; save.SaveStats(); save.SaveProgression();
            save.stats.stats["runs"].value = 34; save.progression.silver = 456; save.SaveStats(); save.SaveProgression();
            string statsBackup = File.ReadAllText(stats + ".bak"), progressionBackup = File.ReadAllText(progress + ".bak");
            File.WriteAllText(stats, SimpleEncryptor.Encrypt("{\"stats\":null}")); SaveManager.loaded = false;
            Check(!save.Load(0, false, out reason, out failPath) && failPath == stats && reason.Contains("initialize stats"), "invalid stats initialization reports stats file");
            Check((GameManager.A_GameOver?.GetInvocationList().Length ?? 0) == initialGameOverListeners + 1, "failed initialization retains one progression listener");
            File.WriteAllText(progress, "{broken encrypted data");
            Check(save.Load(0, true, out reason, out failPath), "encrypted backup recovery: " + reason);
            Check(save.stats.stats["runs"].value == 12 && save.progression.silver == 123, "stats and progression backup values restored");
            Check((GameManager.A_GameOver?.GetInvocationList().Length ?? 0) == initialGameOverListeners + 1, "encrypted recovery replaces progression listener");
            save.SaveStats(); save.SaveProgression();
            Check(File.ReadAllText(stats + ".bak") == statsBackup && File.ReadAllText(progress + ".bak") == progressionBackup, "first encrypted saves preserve backups");
            string[] files = { config, config + ".bak", stats, stats + ".bak", progress, progress + ".bak" }; var before = new string[files.Length];
            for (int i=0; i<files.Length; i++) before[i] = File.ReadAllText(files[i]);
            Action onLoaded = () => { save.SaveConfig(); save.SaveStats(); save.SaveProgression(); };
            SaveManager.A_SavesLoaded += onLoaded;
            try { save.LoadNoSave(); save.SaveConfig(); save.SaveStats(); save.SaveProgression(); }
            finally { SaveManager.A_SavesLoaded -= onLoaded; }
            Check((GameManager.A_GameOver?.GetInvocationList().Length ?? 0) == initialGameOverListeners + 1, "no-save replaces progression listener");
            for (int i=0; i<files.Length; i++) Check(File.ReadAllText(files[i]) == before[i], "no-save preserves file " + files[i]);
            SaveManager.loaded = false; File.Delete(config + ".bak"); File.WriteAllText(config, "{broken");
            Check(!save.Load(0, true, out reason, out failPath) && failPath == config && File.ReadAllText(config) == "{broken", "missing backup does not discard broken primary");
            File.WriteAllText(config, "null");
            Check(!save.Load(0, false, out reason, out failPath) && failPath == config && reason.Contains("empty or null"), "null config identifies the file");
            File.WriteAllText(config + ".bak", "{broken backup");
            Check(!save.Load(0, true, out reason, out failPath) && failPath == config + ".bak", "bad backup identifies the backup path");
            Debug.Log("SaveRecoveryPortCheck passed: backup fallback, backup preservation, no-save event and missing-backup failure");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[]{null});
            pathField.SetValue(null, oldPath); idField.SetValue(null, oldId); testField.SetValue(null, oldTest); SaveManager.loaded = oldLoaded;
            if (hadPref) PlayerPrefs.SetString(pref, oldPref); else PlayerPrefs.DeleteKey(pref);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
