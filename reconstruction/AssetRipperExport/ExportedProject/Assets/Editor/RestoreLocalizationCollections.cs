using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class RestoreLocalizationCollections
{
    public static void Run()
    {
        const string directory = "Assets/Localization/Collections";
        Directory.CreateDirectory(directory);
        var groups = new Dictionary<SharedTableData, List<LocalizationTable>>();
        foreach (string guid in AssetDatabase.FindAssets("t:StringTable", new[] { "Assets/Localization/String Tables" }))
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guid));
            if (table == null || table.SharedData == null || table.SharedData.TableCollectionNameGuid == Guid.Empty) continue;
            if (!groups.TryGetValue(table.SharedData, out var tables)) groups.Add(table.SharedData, tables = new List<LocalizationTable>());
            tables.Add(table);
        }
        int created = 0;
        foreach (var group in groups)
        {
            if (LocalizationEditorSettings.GetCollectionForSharedTableData(group.Key) != null) continue;
            string path = Path.Combine(directory, group.Key.TableCollectionName + ".asset").Replace('\\', '/');
            if (File.Exists(path)) throw new Exception("Collection path already exists: " + path);
            if (LocalizationEditorSettings.CreateCollectionFromLooseTables(group.Value, path) == null)
                throw new Exception("Could not create collection for " + group.Key.TableCollectionName);
            created++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Restored " + created + " editor localization collections from " + groups.Count + " recovered tables");
    }
}
