// Standalone behavior check for the localization store; Unity types are stubbed.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization.Tables;

internal static class LocalizationStoreIntegrityCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "openbonk-localization-" + Guid.NewGuid().ToString("N"));
        try
        {
            var data = new MemoryStream(new byte[] { 1 });
            string first = AndroidOriginalLocalizationStore.Import(data, root);
            Require(AndroidOriginalLocalizationStore.ActiveDirectory(root, "en") == first, "valid generation not selected");
            File.WriteAllBytes(Path.Combine(first, "shared.bundle"), new byte[0]);
            Require(AndroidOriginalLocalizationStore.ActiveDirectory(root, "en") == null, "empty shared bundle accepted");
            string second = AndroidOriginalLocalizationStore.Import(data, root);
            Require(AndroidOriginalLocalizationStore.ActiveDirectory(root, "en") == second, "reimport did not recover");
            File.WriteAllBytes(Path.Combine(second, "strings-en.bundle"), new byte[0]);
            Require(AndroidOriginalLocalizationStore.ActiveDirectory(root, "en") == null, "empty language bundle accepted");
            string third = AndroidOriginalLocalizationStore.Import(data, root);
            AndroidOriginalLocalizationZip.Fail = true;
            try { AndroidOriginalLocalizationStore.Import(data, root); throw new Exception("invalid import succeeded"); }
            catch (InvalidDataException) { }
            Require(AndroidOriginalLocalizationStore.ActiveDirectory(root, "en") == third, "failed import replaced good generation");
            Console.WriteLine("Localization store integrity check passed.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}

public static class AndroidOriginalLocalizationCatalog
{
    public sealed class Language { public string Code, Bundle, StoredFile; public int Entries; }
    public static readonly Language[] All = { new Language { Code = "en", Bundle = "input", StoredFile = "strings-en.bundle", Entries = 51 } };
    public static Language Find(string code) => code == "en" ? All[0] : null;
}
public static class AndroidOriginalLocalizationZip
{
    public static bool Fail;
    public sealed class Result { public byte[] SharedData = { 1 }, Strings = { 2 }; }
    public static Result Convert(Stream input, string bundle) { if (Fail) throw new InvalidDataException(); return new Result(); }
}
public static class AndroidOriginalAtlasZip
{
    public sealed class Atlas { public string Name = "atlas"; }
    public static readonly Atlas[] Atlases = { new Atlas() };
    public static Dictionary<string, byte[]> ExtractAll(Stream input) => new Dictionary<string, byte[]> { { "atlas", new byte[] { 3 } } };
    public static void VerifyRaw(Atlas atlas, byte[] bytes) { if (bytes.Length != 1) throw new InvalidDataException(); }
}
namespace UnityEngine
{
    public static class PlayerPrefs
    {
        private static string value = "";
        public static string GetString(string key, string fallback) => value;
        public static void SetString(string key, string text) { value = text; }
        public static void Save() { }
    }
    public sealed class AssetBundle
    {
        public static AssetBundle LoadFromFile(string path) => new AssetBundle();
        public string[] GetAllAssetNames() { var names = new string[51]; for (int i = 0; i < names.Length; i++) names[i] = i.ToString(); return names; }
        public T LoadAsset<T>(string name) where T : class => (T)(object)new StringTable(name);
        public void Unload(bool all) { }
    }
}
namespace UnityEngine.Localization.Tables
{
    public sealed class StringTable
    {
        public sealed class Entry { public long KeyId = 1; }
        public sealed class Shared { public string TableCollectionName; public Shared(string name) { TableCollectionName = name; } public KeyEntry GetEntry(long id) => new KeyEntry(); }
        public sealed class KeyEntry { public string Key = "key"; }
        public sealed class Identifier { public string Code = "en"; }
        public Shared SharedData;
        public Identifier LocaleIdentifier = new Identifier();
        public Entry[] Values = { new Entry() };
        public StringTable(string name) { SharedData = new Shared(name); }
        public Entry GetEntry(string key) => new Entry();
    }
}
