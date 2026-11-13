#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

public static class AndroidLocalizationConverterCheck
{
    public static void Run()
    {
        string root = "/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data/StreamingAssets/aa/StandaloneLinux64";
        string dest = "/home/your-user/megabonk/build/android";
        foreach (var item in new[] { ("localization-assets-shared_assets_all.bundle", "managed-shared.bundle"), ("localization-string-tables-spanish(es)_assets_all.bundle", "managed-spanish.bundle") })
        {
            byte[] source = File.ReadAllBytes(Path.Combine(root, item.Item1));
            byte[] converted = AndroidLocalizationBundleConverter.Convert(source);
            File.WriteAllBytes(Path.Combine(dest, item.Item2), converted);
            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(dest, item.Item2));
            if (bundle == null) throw new Exception("Editor could not open converted bundle");
            int assetCount = bundle.GetAllAssetNames().Length;
            bundle.Unload(true);
            if (assetCount != 51) throw new Exception("Unexpected asset count: " + assetCount);
            bool rejected = false; try { AndroidLocalizationBundleConverter.Convert(new byte[] { 1, 2, 3 }); } catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("truncated input accepted");
            Debug.Log($"Managed localization converter PASS {item.Item1}: {source.Length} -> {converted.Length}");
        }
    }
}
#endif
