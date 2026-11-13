#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>
/// Reads localization data from the original Linux Addressables bundles. AssetRipper
/// exported placeholder .asset files for these types, so their serialized contents
/// must be recovered from the built bundles before Android Addressables can be rebuilt.
/// </summary>
public static class LocalizationBundleExport
{
	[Serializable]
	private sealed class ExportData
	{
		public List<LocaleData> locales = new List<LocaleData>();
		public List<TableData> tables = new List<TableData>();
		public int assetTableCount;
	}

	[Serializable]
	private sealed class LocaleData
	{
		public string name;
		public string code;
	}

	[Serializable]
	private sealed class TableData
	{
		public string collection;
		public string collectionGuid;
		public string locale;
		public List<EntryData> entries = new List<EntryData>();
	}

	[Serializable]
	private sealed class EntryData
	{
		public long id;
		public string key;
		public string value;
		public bool smart;
	}

	[MenuItem("Megabonk/Export Linux Localization Bundles")]
	public static void ExportLinuxLocalizationBundles()
	{
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string bundleRoot = Path.Combine(projectRoot, "Assets/StreamingAssets/aa/StandaloneLinux64");
		string configured = Environment.GetEnvironmentVariable("OPENBONK_BUILD_ROOT");
		string buildRoot = string.IsNullOrEmpty(configured) ? Path.GetFullPath(Path.Combine(projectRoot, "../../../build")) : Path.GetFullPath(configured);
		string outputPath = Path.Combine(buildRoot, "android/localization-content.json");
		if (File.Exists(outputPath)) throw new IOException("Localization export already exists: " + outputPath);
		string[] bundlePaths = Directory.GetFiles(bundleRoot, "*.bundle");
		Array.Sort(bundlePaths, StringComparer.Ordinal);
		var bundles = new List<AssetBundle>();
		var export = new ExportData();

		try
		{
			foreach (string path in bundlePaths)
			{
				AssetBundle bundle = AssetBundle.LoadFromFile(path);
				if (bundle == null)
					throw new InvalidDataException("Unity could not load localization bundle: " + path);
				bundles.Add(bundle);
			}

			foreach (AssetBundle bundle in bundles)
			{
				foreach (Locale locale in bundle.LoadAllAssets<Locale>())
				{
					if (locale != null)
						export.locales.Add(new LocaleData { name = locale.LocaleName, code = locale.Identifier.Code });
				}

				export.assetTableCount += bundle.LoadAllAssets<AssetTable>().Length;

				foreach (StringTable table in bundle.LoadAllAssets<StringTable>())
				{
					if (table == null || table.SharedData == null)
						continue;

					var tableData = new TableData
					{
						collection = table.SharedData.TableCollectionName,
						collectionGuid = table.SharedData.TableCollectionNameGuid.ToString(),
						locale = table.LocaleIdentifier.Code
					};

					foreach (SharedTableData.SharedTableEntry sharedEntry in table.SharedData.Entries)
					{
						StringTableEntry entry = table.GetEntry(sharedEntry.Id);
						if (entry == null)
							continue;

						tableData.entries.Add(new EntryData
						{
							id = sharedEntry.Id,
							key = sharedEntry.Key,
							value = entry.Value,
							smart = entry.IsSmart
						});
					}

					export.tables.Add(tableData);
				}
			}
		}
		finally
		{
			foreach (AssetBundle bundle in bundles)
				bundle.Unload(true);
		}

		export.tables.Sort((a, b) => string.CompareOrdinal(a.collection + "_" + a.locale, b.collection + "_" + b.locale));
		export.locales.Sort((a, b) => string.CompareOrdinal(a.code, b.code));
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
		File.WriteAllText(outputPath, JsonUtility.ToJson(export, true));
		Debug.Log("Exported " + export.locales.Count + " locales, " + export.tables.Count + " localized string tables, and " + export.assetTableCount + " asset tables to " + outputPath);
		EditorApplication.Exit(0);
	}
}
#endif
