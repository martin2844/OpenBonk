using System;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class BossCurseInteractionPortCheck
{
    public sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }

    public static void Run()
    {
        var previousSettings = LocalizationSettings.Instance;
        var oldInteraction = DetectInteractables.A_Interacted;
        var oldRelease = Enemy.A_EnemyReleasedFromPool;
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BossSpawner.prefab");
        if (prefab == null) throw new Exception("Boss spawner prefab missing");
        var root = UnityEngine.Object.Instantiate(prefab);
        var shrineObject = new GameObject("cursed shrine");
        try
        {
            LocalizationSettings.Instance = settings;
            LocalizationSettings.StringDatabase.TableProvider = new Provider();
            var spawner = root.GetComponent<InteractableBossSpawner>();
            var shrine = shrineObject.AddComponent<InteractableShrineCursed>();
            if (spawner == null || spawner.bossCurseFx == null) throw new Exception("Original boss effect missing");
            spawner.bossCurseFx.SetActive(false);
            typeof(InteractableBossSpawner).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
            DetectInteractables.A_Interacted?.Invoke(shrine, false);
            DetectInteractables.A_Interacted?.Invoke(spawner, true);
            if (spawner.bossCurseFx.activeSelf) throw new Exception("Failed or unrelated interaction activated effect");
            DetectInteractables.A_Interacted?.Invoke(shrine, true);
            if (!spawner.bossCurseFx.activeSelf) throw new Exception("Cursed shrine did not activate boss effect");
            foreach (string code in new[] { "en", "es" })
            {
                LocalizationSettings.SelectedLocale = Locale.CreateLocale(code);
                var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_Interactables_" + code + ".asset");
                var expected = table?.GetEntry("SPAWN_BOSS")?.LocalizedValue;
                if (string.IsNullOrEmpty(expected) || spawner.GetInteractString() != expected) throw new Exception("Boss interaction localization differs for " + code);
            }
            typeof(InteractableBossSpawner).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
            if (DetectInteractables.A_Interacted != oldInteraction || Enemy.A_EnemyReleasedFromPool != oldRelease) throw new Exception("Event subscriptions leaked");
            Debug.Log("BossCurseInteractionPortCheck passed: original boss prefab, curse interaction and English/Spanish strings");
        }
        finally
        {
            DetectInteractables.A_Interacted = oldInteraction;
            Enemy.A_EnemyReleasedFromPool = oldRelease;
            LocalizationSettings.Instance = previousSettings;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(shrineObject);
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
