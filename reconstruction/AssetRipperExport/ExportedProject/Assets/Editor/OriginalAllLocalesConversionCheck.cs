#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class OriginalAllLocalesConversionCheck
{
    public static void Run()
    {
        string root = "/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data/StreamingAssets/aa/StandaloneLinux64";
        string[] files = Directory.GetFiles(root, "localization-string-tables-*_assets_all.bundle"); Array.Sort(files, StringComparer.Ordinal);
        foreach (string file in files)
        {
            byte[] bytes = AndroidLocalizationBundleConverter.Convert(File.ReadAllBytes(file));
            AssetBundle b = AssetBundle.LoadFromMemory(bytes);
            if (b == null) throw new InvalidDataException("Cannot load converted " + Path.GetFileName(file));
            try
            {
                var names = b.GetAllAssetNames(); int count = 0; string locale = null; var keys = new List<string>();
                foreach (string name in names)
                {
                    var table = b.LoadAsset<StringTable>(name);
                    if (table == null) throw new InvalidDataException("Not a string table: " + name);
                    string code = table.LocaleIdentifier.Code;
                    if (locale != null && locale != code) throw new InvalidDataException("Mixed locale"); locale = code;
                    foreach (var entry in table.Values) { keys.Add(name + "\t" + entry.KeyId + "\t" + entry.LocalizedValue); count++; }
                }
                keys.Sort(StringComparer.Ordinal); using var hash = SHA256.Create();
                string digest = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", keys)))).Replace("-", "").ToLowerInvariant();
                Debug.Log($"Original all locales: {Path.GetFileName(file)} code={locale} tables={names.Length} entries={count} sha256={digest}");
            }
            finally { b.Unload(true); }
        }
        Debug.Log("Original all locales DONE count=" + files.Length);
    }
}
#endif
