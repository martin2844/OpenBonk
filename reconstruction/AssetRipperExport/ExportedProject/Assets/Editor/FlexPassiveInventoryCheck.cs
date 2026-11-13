using System;
using System.Globalization;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class FlexPassiveInventoryCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("FlexPassiveInventoryCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("flex passive test"); root.SetActive(false);
        var oldGame = GameManager.Instance; float oldTime = MyTime.time; float oldCryptTime = MyTime.cryptTimer;
        var oldLocale = LocalizationSettings.SelectedLocale; var oldCulture = CultureInfo.CurrentCulture;
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var cooldown = typeof(PassiveAbilityFlex).GetField("cooldown", flags);
        var readyTime = typeof(PassiveAbilityFlex).GetField("flexReadyAtTime", flags);
        float oldCooldown = (float)cooldown.GetValue(null), oldReady = (float)readyTime.GetValue(null);
        var passive = new PassiveAbilityFlex(); int readyEvents = 0;
        Action<bool> onReady = ready => { if (ready) readyEvents++; };
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            var data = AssetDatabase.LoadAssetAtPath<PassiveData>("Assets/MonoBehaviour/Flex.asset");
            Require(data != null, "Flex data asset"); data.Init();
            string description = data.GetDescription();
            Require(!description.Contains("{") && description.Contains("0.2") && description.Contains("2.5%"), "localized description: " + description);
            MyTime.time = 100f; passive.Init(); PassiveAbilityFlex.A_FlexReady += onReady;
            MyTime.time = 104f; passive.Tick(); Require(readyEvents == 0, "strict readiness boundary");
            MyTime.time = 104.1f; passive.Tick(); passive.Tick(); Require(readyEvents == 1, "ready event once");
            PlayerXp.A_LevelUp?.Invoke(10); Require(Mathf.Approximately((float)cooldown.GetValue(null), 10f), "level cooldown");
            PlayerXp.A_LevelUp?.Invoke(100); Require(Mathf.Approximately((float)cooldown.GetValue(null), 4f), "minimum cooldown");
            passive.Cleanup(); PlayerXp.A_LevelUp?.Invoke(0); Require(Mathf.Approximately((float)cooldown.GetValue(null), 4f), "cleanup unsubscribes");
            Debug.Log("FlexPassiveInventoryCheck passed: actual asset description, readiness, cooldown and cleanup");
        }
        finally
        {
            PassiveAbilityFlex.A_FlexReady -= onReady; passive.Cleanup();
            cooldown.SetValue(null, oldCooldown); readyTime.SetValue(null, oldReady);
            MyTime.time = oldTime; MyTime.cryptTimer = oldCryptTime;
            LocalizationSettings.SelectedLocale = oldLocale; CultureInfo.CurrentCulture = oldCulture;
            GameManager.Instance = oldGame; UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
