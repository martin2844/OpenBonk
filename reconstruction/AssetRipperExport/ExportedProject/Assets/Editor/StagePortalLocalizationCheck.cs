using System;
using Assets.Scripts.UI.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class StagePortalLocalizationCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity");
            var portal = UnityEngine.Object.FindAnyObjectByType<InteractablePortal>(FindObjectsInactive.Include);
            if (portal == null) throw new Exception("Original scene portal missing");
            var merchantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShadyGuy.prefab");
            var merchant = merchantPrefab != null ? merchantPrefab.GetComponentInChildren<InteractableShadyGuy>(true) : null;
            if (merchant == null) throw new Exception("Original Shady Guy prefab missing");
            LocalizationSettings.StringDatabase.TableProvider = new Provider();
            LocalizationSettings.SelectedLocale = Locale.CreateLocale("en");
            string english = portal.GetInteractString();
            string merchantEnglish = merchant.GetInteractString();
            LocalizationSettings.SelectedLocale = Locale.CreateLocale("es");
            string spanish = portal.GetInteractString();
            string merchantSpanish = merchant.GetInteractString();
            if (english != "Enter Portal" || spanish != "Entra al Portal")
                throw new Exception("Unexpected portal localization: en=" + english + ", es=" + spanish);
            if (merchantEnglish != "Talk to Shady Guy" || merchantSpanish != "Habla con el tipo raro")
                throw new Exception("Unexpected merchant localization: en=" + merchantEnglish + ", es=" + merchantSpanish);
            Debug.Log("StagePortalLocalizationCheck passed: original portal and merchant, exported English/Spanish tables");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
