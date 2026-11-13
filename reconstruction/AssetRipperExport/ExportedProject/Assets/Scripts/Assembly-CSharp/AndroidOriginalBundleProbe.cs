#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Localization.Tables;

// Intent-gated compatibility investigation. Runs before normal scenes load,
// using disposable files in this app's external Probe directory.
internal static class AndroidOriginalBundleProbe
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_original_bundle_probe", false)) return;
        if (intent.Call<bool>("getBooleanExtra", "megabonk_zip_store_probe", false))
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Probe");
                string root = Path.Combine(Application.persistentDataPath, "OriginalLocalization");
                using var zip = File.OpenRead(Path.Combine(dir, "source.zip"));
                string installed = AndroidOriginalLocalizationStore.Import(zip, root);
                bool rejected = false;
                try { using var invalid = new MemoryStream(new byte[] { 1, 2, 3 }); AndroidOriginalLocalizationStore.Import(invalid, root); }
                catch (InvalidDataException) { rejected = true; }
                bool preserved = installed == AndroidOriginalLocalizationStore.ActiveDirectory(root);
                bool latam = installed == AndroidOriginalLocalizationStore.ActiveDirectory(root, "es-419");
                if (!rejected || !preserved || !latam) throw new InvalidDataException("Failed import changed active generation or omitted Latin American Spanish");
                File.Copy(Path.Combine(installed, "strings.bundle"), Path.Combine(dir, "linux.bundle"), true);
                File.Copy(Path.Combine(installed, "shared.bundle"), Path.Combine(dir, "shared.bundle"), true);
                Debug.Log($"Original localization store probe PASS rejected={rejected} preserved={preserved} latam={latam} generation={Path.GetFileName(installed)}");
            }
            catch (Exception e) { Debug.LogError("Original localization store probe FAILED " + e); return; }
        }
        if (intent.Call<bool>("getBooleanExtra", "megabonk_read_store_probe", false))
        {
            string active = AndroidOriginalLocalizationStore.ActiveDirectory(Path.Combine(Application.persistentDataPath, "OriginalLocalization"));
            Debug.Log($"Original localization store restart active={(active == null ? "NONE" : Path.GetFileName(active))}");
        }
        if (intent.Call<bool>("getBooleanExtra", "megabonk_zip_conversion_probe", false))
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Probe");
                using var input = File.OpenRead(Path.Combine(dir, "source.zip"));
                var converted = AndroidOriginalLocalizationZip.Convert(input, "localization-string-tables-spanish(es)_assets_all.bundle");
                File.WriteAllBytes(Path.Combine(dir, "linux.bundle"), converted.Strings);
                File.WriteAllBytes(Path.Combine(dir, "shared.bundle"), converted.SharedData);
                Debug.Log($"Original ZIP conversion PASS: strings={converted.Strings.Length}, shared={converted.SharedData.Length}");
            }
            catch (Exception e) { Debug.LogError("Original ZIP conversion FAILED " + e); return; }
        }
        if (intent.Call<bool>("getBooleanExtra", "megabonk_managed_conversion_probe", false))
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Probe");
                foreach (var item in new[] { ("linux-source.bundle", "linux.bundle"), ("shared-source.bundle", "shared.bundle") })
                {
                    byte[] input = File.ReadAllBytes(Path.Combine(dir, item.Item1));
                    byte[] converted = AndroidLocalizationBundleConverter.Convert(input);
                    File.WriteAllBytes(Path.Combine(dir, item.Item2), converted);
                    Debug.Log($"Managed Android localization conversion {item.Item1}: {input.Length} -> {converted.Length}");
                }
            }
            catch (Exception e) { Debug.LogError("Managed Android localization conversion FAILED " + e); return; }
        }
        foreach (string name in new[] { "linux.bundle", "android.bundle" })
        {
            AssetBundle bundle = null;
            AssetBundle shared = null;
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "Probe", name);
                string sharedPath = Path.Combine(Application.persistentDataPath, "Probe", "shared.bundle");
                if (name == "linux.bundle" && File.Exists(sharedPath))
                {
                    shared = AssetBundle.LoadFromFile(sharedPath);
                    Debug.Log($"Original bundle probe shared={(shared == null ? "LOAD_NULL" : "LOADED")}");
                }
                bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null) { Debug.LogWarning("Original bundle probe " + name + " LOAD_NULL"); continue; }
                var names = bundle.GetAllAssetNames(); Array.Sort(names, StringComparer.Ordinal);
                foreach (var assetName in names)
                {
                    var asset = bundle.LoadAsset<UnityEngine.Object>(assetName);
                    if (asset is StringTable table)
                    {
                        var entries = new List<string>();
                        foreach (var entry in table.Values) entries.Add(entry.KeyId + "\t" + entry.LocalizedValue);
                        entries.Sort(StringComparer.Ordinal);
                        using var hash = SHA256.Create();
                        string digest = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", entries)))).Replace("-", "").ToLowerInvariant();
                        Debug.Log($"Original bundle probe {name} table={assetName} entries={entries.Count} contentSha256={digest}");
                        if (name == "linux.bundle" && shared != null)
                        {
                            var sharedData = table.SharedData;
                            int resolved = 0, missing = 0;
                            if (sharedData != null)
                                foreach (var key in sharedData.Entries)
                                {
                                    var byId = table.GetEntry(key.Id);
                                    if (byId == null) continue;
                                    var byName = table.GetEntry(key.Key);
                                    if (byName != null && byName.KeyId == key.Id && byName.LocalizedValue == byId.LocalizedValue) resolved++;
                                    else missing++;
                                }
                            Debug.Log($"Original bundle probe references {assetName} shared={(sharedData != null)} resolved={resolved} missing={missing}");
                        }
                    }
                    else Debug.Log($"Original bundle probe {name} asset={assetName} type={(asset == null ? "null" : asset.GetType().FullName)}");
                }
                Debug.Log($"Original bundle probe {name} assets={names.Length} DONE");
            }
            catch (Exception e) { Debug.LogWarning($"Original bundle probe {name} FAILED {e}"); }
            finally { if (bundle != null) bundle.Unload(true); if (shared != null) shared.Unload(true); }
        }
    }
}
#endif
