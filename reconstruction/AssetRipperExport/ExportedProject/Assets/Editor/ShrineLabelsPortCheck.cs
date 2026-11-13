using System;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class ShrineLabelsPortCheck
{
    public static void Run()
    {
        var previousSettings = LocalizationSettings.Instance;
        var previousEntries = InteractablesStatus.interactablesByName;
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        GameObject curse = null, magnet = null;
        try
        {
            LocalizationSettings.Instance = settings;
            LocalizationSettings.StringDatabase.TableProvider = new BossCurseInteractionPortCheck.Provider();
            var cursePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CursedShrine.prefab");
            var magnetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MagnetShrine.prefab");
            if (cursePrefab == null || magnetPrefab == null) throw new Exception("Original shrine prefabs missing");
            curse = UnityEngine.Object.Instantiate(cursePrefab);
            magnet = UnityEngine.Object.Instantiate(magnetPrefab);
            var cursed = curse.GetComponent<InteractableShrineCursed>();
            var magnetic = magnet.GetComponent<InteractableShrineMagnet>();
            if (cursed == null || magnetic == null || !cursed.ShowInDebug() || !magnetic.ShowInDebug() ||
                cursed.GetDebugName() != "Boss Curses" || magnetic.GetDebugName() != "Magnet Shrines")
                throw new Exception("Shrine tracking metadata differs");
            foreach (string code in new[] { "en", "es" })
            {
                LocalizationSettings.SelectedLocale = Locale.CreateLocale(code);
                var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_Interactables_" + code + ".asset");
                if (cursed.GetInteractString() != table.GetEntry("SHRINE_CURSED").LocalizedValue ||
                    magnetic.GetInteractString() != table.GetEntry("SHRINE_MAGNET").LocalizedValue)
                    throw new Exception("Shrine localized label differs for " + code);
            }
            InteractablesStatus.interactablesByName = new System.Collections.Generic.Dictionary<string, InteractablesStatus.InteractableStatusContainer>();
            InteractablesStatus.Init();
            BaseInteractable.A_DebugSpawn?.Invoke(cursed.GetDebugName());
            BaseInteractable.A_DebugSpawn?.Invoke(magnetic.GetDebugName());
            DetectInteractables.A_Interacted?.Invoke(cursed, true);
            DetectInteractables.A_Interacted?.Invoke(magnetic, false);
            if (InteractablesStatus.interactablesByName["Boss Curses"].numUsed != 1 ||
                InteractablesStatus.interactablesByName["Magnet Shrines"].numUsed != 0)
                throw new Exception("Shrine use tracking differs");
            Debug.Log("ShrineLabelsPortCheck passed: original prefabs, English/Spanish text and shrine tracking");
        }
        finally
        {
            InteractablesStatus.Cleanup();
            InteractablesStatus.interactablesByName = previousEntries;
            LocalizationSettings.Instance = previousSettings;
            if (curse != null) UnityEngine.Object.DestroyImmediate(curse);
            if (magnet != null) UnityEngine.Object.DestroyImmediate(magnet);
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
