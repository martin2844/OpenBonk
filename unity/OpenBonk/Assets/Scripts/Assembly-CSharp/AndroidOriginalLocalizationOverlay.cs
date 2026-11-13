using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

// Experimental in-memory merge, invoked only by diagnostics for now.
// Keep port-specific control translations and existing table metadata.
public static class AndroidOriginalLocalizationOverlay
{
    private sealed class Change { public StringTable Table; public long Id; public string Value; }
    public static int Apply(IEnumerable<StringTable> originalTables, Locale locale)
    {
        if (originalTables == null || locale == null) throw new ArgumentNullException();
        var changes = new List<Change>(); var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in originalTables)
        {
            if (source == null || source.SharedData == null) throw new InvalidDataException("Missing shared table data");
            string name = source.SharedData.TableCollectionName;
            if (string.IsNullOrEmpty(name) || !names.Add(name)) throw new InvalidDataException("Invalid or duplicate table name");
            if (name == "SettingsControls") continue;
            if (source.LocaleIdentifier.Code != locale.Identifier.Code) throw new InvalidDataException("Locale mismatch");
            var target = LocalizationSettings.StringDatabase.GetTable(name, locale);
            if (target == null || target.SharedData == null) throw new InvalidDataException("Missing target table: " + name);
            foreach (var entry in source.Values)
            {
                // Empty source values should not erase a port translation or
                // prevent valid tables from loading due to an orphaned key.
                if (string.IsNullOrEmpty(entry.LocalizedValue)) continue;
                var sourceKey = source.SharedData.GetEntry(entry.KeyId);
                var targetKey = target.SharedData.GetEntry(entry.KeyId);
                if (sourceKey == null) throw new InvalidDataException("Missing original key in " + name + ": " + entry.KeyId);
                // Reconstructed/port-specific tables can lack original keys.
                // Only replace entries whose identity exists in both tables.
                if (targetKey == null || sourceKey.Key != targetKey.Key || target.GetEntry(entry.KeyId) == null) continue;
                changes.Add(new Change { Table = target, Id = entry.KeyId, Value = entry.LocalizedValue });
            }
        }
        if (changes.Count == 0) throw new InvalidDataException("No matching localization entries");
        foreach (var change in changes) change.Table.AddEntry(change.Id, change.Value);
        return changes.Count;
    }
}
