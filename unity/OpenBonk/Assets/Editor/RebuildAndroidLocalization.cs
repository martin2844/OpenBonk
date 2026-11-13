#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>Restores extracted Localization assets and builds their Android Addressables.</summary>
public static class RebuildAndroidLocalization
{
	[Serializable]
	private sealed class ExportData
	{
		public List<LocaleData> locales;
		public List<TableData> tables;
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
		public List<EntryData> entries;
	}

	[Serializable]
	private sealed class EntryData
	{
		public long id;
		public string key;
		public string value;
		public bool smart;
	}

	[MenuItem("Megabonk/Rebuild Android Localization")]
	public static void Build()
	{
		try
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string configured = Environment.GetEnvironmentVariable("OPENBONK_BUILD_ROOT");
			string buildRoot = string.IsNullOrEmpty(configured) ? Path.GetFullPath(Path.Combine(projectRoot, "../../../build")) : Path.GetFullPath(configured);
			string exportPath = Path.Combine(buildRoot, "android/localization-content.json");
			ExportData data = JsonUtility.FromJson<ExportData>(File.ReadAllText(exportPath));
			if (data == null || data.locales == null || data.tables == null || data.tables.Count == 0)
				throw new InvalidDataException("Localization export is empty: " + exportPath);
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
			if (settings == null)
				throw new InvalidOperationException("Could not create Addressables settings.");
			foreach (AddressableAssetGroup group in settings.groups.ToArray())
			{
				if (group != null && (group.Name == "Localization-Locales" || group.Name == "Localization-Assets-Shared" || group.Name.StartsWith("Localization-String-Tables-", StringComparison.Ordinal)))
					settings.RemoveGroup(group);
			}

			var locales = new Dictionary<string, Locale>(StringComparer.OrdinalIgnoreCase);
			string localesDirectory = "Assets/Localization/Locales";
			if (!Directory.Exists(localesDirectory))
			{
				Directory.CreateDirectory(localesDirectory);
				AssetDatabase.Refresh();
			}
			foreach (LocaleData localeData in data.locales)
			{
				string assetPath = FindLocaleAsset(localesDirectory, localeData.code, localeData.name);
				Locale locale = GetOrReplaceAsset<Locale>(assetPath);

				locale.Identifier = new LocaleIdentifier(localeData.code);
				locale.LocaleName = localeData.name;
				EditorUtility.SetDirty(locale);
				locales.Add(localeData.code, locale);
				LocalizationEditorSettings.AddLocale(locale);
			}

			var tablesByCollection = data.tables.GroupBy(t => t.collection).ToDictionary(g => g.Key, g => g.ToList());
			var sharedDataByCollection = new Dictionary<string, SharedTableData>(StringComparer.Ordinal);
			foreach (KeyValuePair<string, List<TableData>> collection in tablesByCollection)
			{
				TableData sourceTable = collection.Value[0];
				string sharedPath = "Assets/Localization/String Tables/" + collection.Key + " Shared Data.asset";
				SharedTableData shared = GetOrReplaceAsset<SharedTableData>(sharedPath);

				shared.TableCollectionName = collection.Key;
				shared.Clear();
				foreach (EntryData entry in sourceTable.entries)
					shared.AddKey(entry.key, entry.id);

				PropertyInfo guidProperty = typeof(SharedTableData).GetProperty("TableCollectionNameGuid");
				guidProperty.GetSetMethod(true).Invoke(shared, new object[] { Guid.Parse(sourceTable.collectionGuid) });
				EditorUtility.SetDirty(shared);
				sharedDataByCollection.Add(collection.Key, shared);
			}

			AddressableAssetGroup sharedGroup = GetOrCreateGroup(settings, "Localization-Assets-Shared");
			foreach (KeyValuePair<string, SharedTableData> pair in sharedDataByCollection)
			{
				string path = AssetDatabase.GetAssetPath(pair.Value);
				AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), sharedGroup, false, false);
				entry.address = pair.Key + " Shared Data";
				EditorUtility.SetDirty(entry.parentGroup);
			}

			foreach (TableData tableData in data.tables)
			{
				if (!locales.TryGetValue(tableData.locale, out Locale locale))
					throw new InvalidDataException("No Locale asset for " + tableData.locale);

				string tablePath = "Assets/Localization/String Tables/" + tableData.collection + "_" + tableData.locale + ".asset";
				StringTable table = GetOrReplaceAsset<StringTable>(tablePath);

				table.SharedData = sharedDataByCollection[tableData.collection];
				table.LocaleIdentifier = locale.Identifier;
				table.Clear();
				table.name = tableData.collection + "_" + tableData.locale;
				foreach (EntryData entryData in tableData.entries)
				{
					StringTableEntry entry = table.AddEntry(entryData.id, entryData.value ?? string.Empty);
					entry.IsSmart = entryData.smart;
				}
				EditorUtility.SetDirty(table);

				settings.AddLabel("Locale-" + tableData.locale);
				string groupName = "Localization-String-Tables-" + locale.LocaleName;
				AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);
				AddressableAssetEntry addressableEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(tablePath), group, false, false);
				addressableEntry.address = tableData.collection + "_" + tableData.locale;
				addressableEntry.SetLabel("Locale-" + tableData.locale, true, true, false);
			}

			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult buildResult);
			if (!string.IsNullOrEmpty(buildResult.Error))
				throw new InvalidOperationException("Android Addressables build failed: " + buildResult.Error);

			Debug.Log("Restored " + data.locales.Count + " locales and " + data.tables.Count + " string tables from " + exportPath + "; Android Addressables built.");
			EditorApplication.Exit(0);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			EditorApplication.Exit(1);
		}
	}

	private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string name)
	{
		AddressableAssetGroup group = settings.FindGroup(name);
		if (group == null)
		{
			group = settings.CreateGroup(name, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			group.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
		}
		return group;
	}

	private static T GetOrReplaceAsset<T>(string assetPath) where T : ScriptableObject
	{
		T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
		if (asset != null)
			return asset;

		string fullPath = Path.GetFullPath(assetPath);
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string configured = Environment.GetEnvironmentVariable("OPENBONK_BUILD_ROOT");
		string buildRoot = string.IsNullOrEmpty(configured) ? Path.GetFullPath(Path.Combine(projectRoot, "../../../build")) : Path.GetFullPath(configured);
		string backupRoot = Path.Combine(buildRoot, "localization-placeholders");
		string backupPath = Path.Combine(backupRoot, assetPath.Substring("Assets/".Length));
		Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
		if (File.Exists(fullPath))
		{
			File.Copy(fullPath, backupPath, true);
			if (File.Exists(fullPath + ".meta"))
				File.Copy(fullPath + ".meta", backupPath + ".meta", true);
			AssetDatabase.DeleteAsset(assetPath);
		}

		string assetDirectory = Path.GetDirectoryName(fullPath);
		if (!Directory.Exists(assetDirectory))
		{
			Directory.CreateDirectory(assetDirectory);
			AssetDatabase.Refresh();
		}
		asset = ScriptableObject.CreateInstance<T>();
		AssetDatabase.CreateAsset(asset, assetPath);
		return asset;
	}

	private static string FindLocaleAsset(string directory, string code, string localeName)
	{
		string exactName = directory + "/" + localeName + ".asset";
		if (File.Exists(exactName))
			return exactName;

		string[] candidates = Directory.GetFiles(directory, "*.asset");
		Array.Sort(candidates, (a, b) => a.Length.CompareTo(b.Length));
		foreach (string path in candidates)
		{
			Match match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"\(([^()]*)\)(?: \([^()]*\))?$");
			if (match.Success && string.Equals(match.Groups[1].Value, code, StringComparison.OrdinalIgnoreCase))
				return path;
		}
		return directory + "/" + Sanitize(localeName) + " (" + code + ").asset";
	}

	private static string Sanitize(string value)
	{
		foreach (char invalid in Path.GetInvalidFileNameChars())
			value = value.Replace(invalid, '_');
		return value;
	}
}
#endif
