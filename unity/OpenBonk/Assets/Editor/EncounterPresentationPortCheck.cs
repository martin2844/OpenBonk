using System;
using System.Reflection;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class EncounterPresentationPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.SelectedLocale = Locale.CreateLocale("en");
        LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var root = new GameObject("presentation test"); root.SetActive(false);
        var shrine = root.AddComponent<InteractableShrineMoai>();
        string shrinePrompt = LocalizationUtility.GetLocalizedString("Game_Interactables", "SHRINE_MOAI");
        Require(shrine.GetInteractString() == shrinePrompt && !shrinePrompt.StartsWith("Missing localization") && shrine.ShowInDebug() && shrine.GetDebugName() == "Moais", "Moai shrine prompt and debug identity");
        var screen = root.AddComponent<LevelupScreen>();
        var picker = root.AddComponent<UpgradePicker>(); screen.upgradePicker = picker;
        screen.effects = new GameObject("effects");
        screen.t_title = new GameObject("title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        screen.t_description = new GameObject("description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        var type = typeof(LevelupScreen).GetField("encounterType", Private);
        var configure = typeof(LevelupScreen).GetMethod("ConfigureEncounterPresentation", Private);
        var luckField = typeof(UpgradePicker).GetField("moaiLuckMode", Private);
        type.SetValue(screen, EEncounter.Levelup); configure.Invoke(screen, null);
        Require(screen.t_title.text == LocalizationUtility.GetLocalizedString("Game_Ui", "HEADER_LEVELUP") && !screen.t_description.gameObject.activeSelf && screen.effects.activeSelf, "normal level presentation");
        type.SetValue(screen, EEncounter.Moai);
        bool rare = false;
        for (int seed = 0; seed < 1000; seed++)
        {
            UnityEngine.Random.InitState(seed);
            int expected = UnityEngine.Random.Range(0, 3);
            if (UnityEngine.Random.Range(0f, 1f) <= 0.01f) expected = 3;
            int line = UnityEngine.Random.Range(0, 3);
            UnityEngine.Random.InitState(seed); configure.Invoke(screen, null);
            string description = LocalizationUtility.GetLocalizedString("Game_Moai", $"MOAI_{expected}_{line}");
            Require((int)luckField.GetValue(picker) == expected && screen.t_description.text == description && !description.StartsWith("Missing localization"), "Moai luck and matching description");
            Require(screen.t_title.text == LocalizationUtility.GetLocalizedString("Game_Ui", "HEADER_MOAI") && screen.t_description.gameObject.activeSelf && screen.effects.activeSelf, "Moai presentation");
            rare |= expected == 3;
        }
        Require(rare, "rare Moai mode reached");
        type.SetValue(screen, EEncounter.ShadyGuy); UnityEngine.Random.InitState(51);
        int shadyLine = UnityEngine.Random.Range(0, 14);
        UnityEngine.Random.InitState(51); configure.Invoke(screen, null);
        string shady = LocalizationUtility.GetLocalizedString("Game_ShadyGuy", $"SHADY_GUY_{shadyLine}");
        Require(screen.t_title.text == LocalizationUtility.GetLocalizedString("Game_Ui", "HEADER_SHADY") && screen.t_description.text == shady && !shady.StartsWith("Missing localization") && screen.t_description.gameObject.activeSelf && !screen.effects.activeSelf, "Shady Guy presentation");
        Debug.Log("EncounterPresentationPortCheck passed: localized headings, all Moai modes and matching text, Shady Guy text and effects");
        UnityEngine.Object.DestroyImmediate(screen.t_title.gameObject); UnityEngine.Object.DestroyImmediate(screen.t_description.gameObject);
        UnityEngine.Object.DestroyImmediate(screen.effects); UnityEngine.Object.DestroyImmediate(root);
    }
}
