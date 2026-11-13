using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization.Tables;

// Experimental store. Published generations are immutable; the selected
// generation changes only after conversion and semantic validation succeed.
public static class AndroidOriginalLocalizationStore
{
    private const string Preference = "megabonk.original_localization.es.generation";
    public static string ActiveDirectory(string root, string locale = "es")
    {
        string name = PlayerPrefs.GetString(Preference, "");
        if (name.Length != 34 || !name.StartsWith("g-", StringComparison.Ordinal) || !Guid.TryParseExact(name.Substring(2), "N", out _)) return null;
        string path = Path.Combine(root, name);
        string strings = AndroidOriginalLocalizationCatalog.Find(locale)?.StoredFile;
        return strings != null && File.Exists(Path.Combine(path, "shared.bundle")) && File.Exists(Path.Combine(path, strings)) ? path : null;
    }
    public static string Import(Stream input, string root)
    {
        // Convert fully before writing anything to the store.
        var bundles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        byte[] sharedBytes = null;
        foreach (var language in AndroidOriginalLocalizationCatalog.All)
        {
            input.Position = 0;
            var converted = AndroidOriginalLocalizationZip.Convert(input, language.Bundle);
            if (sharedBytes == null) sharedBytes = converted.SharedData;
            else
            {
                if (sharedBytes.Length != converted.SharedData.Length) throw new InvalidDataException("Shared bundle mismatch");
                for (int i = 0; i < sharedBytes.Length; i++) if (sharedBytes[i] != converted.SharedData[i]) throw new InvalidDataException("Shared bundle mismatch");
            }
            bundles.Add(language.StoredFile, converted.Strings);
        }
        var atlases = AndroidOriginalAtlasZip.ExtractAll(input);
        Directory.CreateDirectory(root);
        string id = "g-" + Guid.NewGuid().ToString("N");
        string stage = Path.Combine(root, "." + id + ".partial"), final = Path.Combine(root, id);
        bool published = false;
        try
        {
            Directory.CreateDirectory(stage);
            File.WriteAllBytes(Path.Combine(stage, "shared.bundle"), sharedBytes);
            foreach (var bundle in bundles) File.WriteAllBytes(Path.Combine(stage, bundle.Key), bundle.Value);
            foreach (var atlas in atlases) File.WriteAllBytes(Path.Combine(stage, atlas.Key + ".raw"), atlas.Value);
            Validate(stage);
            Directory.Move(stage, final);
            string previous = PlayerPrefs.GetString(Preference, "");
            try { PlayerPrefs.SetString(Preference, id); PlayerPrefs.Save(); published = true; }
            catch { PlayerPrefs.SetString(Preference, previous); PlayerPrefs.Save(); throw; }
            return final;
        }
        finally
        {
            if (Directory.Exists(stage)) Directory.Delete(stage, true);
            if (!published && Directory.Exists(final)) Directory.Delete(final, true);
        }
    }
    private static void Validate(string dir)
    {
        foreach (var atlas in AndroidOriginalAtlasZip.Atlases) AndroidOriginalAtlasZip.VerifyRaw(atlas, File.ReadAllBytes(Path.Combine(dir, atlas.Name + ".raw")));
        AssetBundle shared = null;
        try
        {
            shared = AssetBundle.LoadFromFile(Path.Combine(dir, "shared.bundle"));
            if (shared == null) throw new InvalidDataException("Shared localization bundle could not load");
            foreach (var language in AndroidOriginalLocalizationCatalog.All) ValidateStrings(dir, language.StoredFile, language.Code, language.Entries);
        }
        finally { if (shared != null) shared.Unload(true); }
    }
    private static void ValidateStrings(string dir, string file, string locale, int expectedEntries)
    {
        AssetBundle strings = null;
        try
        {
            strings = AssetBundle.LoadFromFile(Path.Combine(dir, file));
            if (strings == null) throw new InvalidDataException("String localization bundle could not load");
            var names = strings.GetAllAssetNames();
            if (names.Length != 51) throw new InvalidDataException("Unexpected table count");
            int entries = 0; var tableNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                var table = strings.LoadAsset<StringTable>(name);
                if (table == null || table.SharedData == null || table.LocaleIdentifier.Code != locale || !tableNames.Add(table.SharedData.TableCollectionName)) throw new InvalidDataException("Invalid localization table");
                foreach (var entry in table.Values)
                {
                    var key = table.SharedData.GetEntry(entry.KeyId);
                    if (key == null || table.GetEntry(key.Key)?.KeyId != entry.KeyId) throw new InvalidDataException("Invalid localization reference");
                    entries++;
                }
            }
            if (entries != expectedEntries) throw new InvalidDataException("Unexpected translated entry count");
        }
        finally { if (strings != null) strings.Unload(true); }
    }
}
