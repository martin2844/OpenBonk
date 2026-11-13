using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.InGame.Rewards.Effects;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Mouse;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.UI.Menu.Windows;

public static class EncounterOffersPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("EncounterOffersPortCheck: " + message); }
    public static void Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var root = new GameObject("encounter fixture"); root.SetActive(false);
        GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); stats.stats[EStat.Luck] = 1f; inventory.playerStats = stats; inventory.statInventory = new StatInventory(); inventory.itemInventory = new ItemInventory();
        Require(EncounterUtility.upgradableStatsShrines.Count == 28 && EncounterUtility.upgradableStatsChaosAndGamble.Count == 27, "recovered pool lengths");
        Require(EncounterUtility.upgradableStatsShrines.Contains(EStat.JumpHeight) && !EncounterUtility.upgradableStatsChaosAndGamble.Contains(EStat.JumpHeight), "pool difference");
        var shrine = root.AddComponent<ChargeShrine>(); ChargeShrine.lastRewardShrine = shrine;
        var randomData = AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/MonoBehaviour/RandomStatsEncounter.asset");
        var balanceData = AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/MonoBehaviour/ShrineOfBalance.asset");
        Require(randomData.HasRarity() && !balanceData.HasRarity() && balanceData.GetOffers().Length == 2, "original encounter data generates offers");
        typeof(ChargeShrine).GetField("<isGolden>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shrine, true);
        global::Utility.MyRandom.random = new System.Random(71); UnityEngine.Random.InitState(11);
        var baseOffers = randomData.GetOffers();
        Require(baseOffers.Length == 3 && Array.TrueForAll(baseOffers, offer => offer.rarity == ERarity.Legendary), "golden shrine forces legendary offers");
        var wrench = new ItemWrench(inventory.itemInventory) { amount = 2 }; inventory.itemInventory.items[EItem.Wrench] = wrench;
        global::Utility.MyRandom.random = new System.Random(71); UnityEngine.Random.InitState(11);
        var wrenchOffers = randomData.GetOffers();
        for (int i = 0; i < 3; i++) Require(Mathf.Abs(wrenchOffers[i].effects[0].statModifier.modification - baseOffers[i].effects[0].statModifier.modification * 1.15f) < .0001f, "wrench scales original shrine offers");
        global::Utility.MyRandom.random = new System.Random(314159);
        var picks = EncounterUtility.GetRandomStats(28);
        Require(new HashSet<EStat>(picks).SetEquals(EncounterUtility.upgradableStatsShrines), "all shrine stats drawn without replacement");
        global::Utility.MyRandom.random = new System.Random(314159);
        var offers = EncounterUtility.GetRandomStatOffers(28, true);
        Require(offers.Count == picks.Count, "one offer per drawn stat");
        for (int i = 0; i < offers.Count; i++)
        {
            var offer = offers[i]; var effect = offer.effects[0];
            Require(offer.rarity == ERarity.Legendary && offer.effects.Length == 1 && effect.effectType == EEncounterEffect.StatChange && effect.permanent && effect.isPositiveEffect, "offer defaults");
            Require(effect.statModifier.stat == picks[i], "seeded selection order");
            if (picks[i] == EStat.DamageMultiplier) Require(effect.statModifier.modifyType == EStatModifyType.Addition && Mathf.Abs(effect.statModifier.modification - .24f) < .0001f, "damage value and rarity multiplier");
            if (picks[i] == EStat.MaxHealth) Require(effect.statModifier.modifyType == EStatModifyType.Flat && effect.statModifier.modification == 30f, "health value and rarity multiplier");
        }
        global::Utility.MyRandom.random = new System.Random(13);
        var balance = EncounterUtility.GetBalanceShrineOffers(3);
        var used = new HashSet<EStat>();
        foreach (var offer in balance)
        {
            Require(offer.rarity == ERarity.Rare && offer.effects.Length == 2, "balance offer shape");
            Require(offer.effects[0].effectType == EEncounterEffect.StatChangeBalanceShrine && offer.effects[1].effectType == EEncounterEffect.StatChangeBalanceShrine, "balance effect type");
            Require(offer.effects[0].statModifier.modifyType == EStatModifyType.Multiplication && offer.effects[0].statModifier.modification == 2f && offer.effects[1].statModifier.modifyType == EStatModifyType.Multiplication && offer.effects[1].statModifier.modification == .5f, "balance multipliers");
            Require(used.Add(offer.effects[0].statModifier.stat) && used.Add(offer.effects[1].statModifier.stat), "balance selections are distinct");
        }
        var selected = offers[0];
        Require(selected.CanAccept(out string reason) && reason == "", "stat offer can be accepted");
        Require(selected.GetEffectsDescription().StartsWith("Gain <b><color=#24ff36>"), "localized stat offer description");
        string balanceText = balance[0].GetEffectsDescription();
        var uiTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_Ui_en.asset");
        Require(balanceText.StartsWith(uiTable.GetEntry("BALANCE_SHRINE_TEXT").GetLocalizedString() + "\n\nDouble ") && balanceText.Contains("\nHalve "), "balance description and line breaks");
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.maxHp = 100; health.hp = 25; inventory.playerHealth = health;
        var cost = new EffectStat { effectType = EEncounterEffect.EHealth, value = -.25f };
        var priced = new EncounterOffer { effects = new[] { selected.effects[0], cost } };
        Require(!priced.CanAccept(out reason) && reason == "Not enough HP", "health cost cannot consume remaining health");
        health.hp = 26;
        Require(priced.CanAccept(out reason) && reason == "", "health cost permits surviving player");
        cost.effectType = EEncounterEffect.EGold; cost.value = -5f;
        Require(!priced.CanAccept(out reason) && reason == "Not enough gold", "insufficient gold rejects offer");
        typeof(PlayerInventory).GetField("<gold>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inventory, 5f);
        Require(priced.CanAccept(out reason) && reason == "", "equal gold permits offer");
        Require(new EffectStat { effectType = EEncounterEffect.EHealth, value = .25f }.GetDescription() == "Heal <b><color=#24ff36>25%</color></b> HP of max HP", "health effect description");
        selected.ApplyEffects(false);
        var selectedModifier = selected.effects[0].statModifier;
        Require(inventory.statInventory.permanentChanges[selectedModifier.stat][0] == selectedModifier, "selected stat offer applies to inventory");
        balance[0].ApplyEffects(false);
        foreach (var effect in balance[0].effects)
            Require(inventory.statInventory.permanentChanges[effect.statModifier.stat].Contains(effect.statModifier), "both balance effects apply");
        var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
        var dictionary = (Dictionary<EEncounter, EncounterData>)typeof(DataManager).GetField("encounterData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dataManager);
        dictionary[EEncounter.BalanceShrine] = balanceData;
        var originalEncounterName = balanceData.localizedName; var originalEncounterDescription = balanceData.localizedDescription;
        balanceData.localizedName = new LocalizedString("Game_Ui", "SHRINE_CHARGE_NAME"); balanceData.localizedDescription = new LocalizedString("Game_Ui", "SHRINE_BALANCE_DESC");
        var uiManager = root.AddComponent<UiManager>(); UiManager.Instance = uiManager; uiManager.shrineLogs = root.AddComponent<ShrineLogs>(); uiManager.scoreUi = root.AddComponent<ScoreUi>(); uiManager.encounterWindows = root.AddComponent<EncounterWindows>();
        var windowObject = new GameObject("encounter test", typeof(RectTransform)); windowObject.SetActive(false);
        var window = windowObject.AddComponent<EncounterUi>(); window.t_name = new GameObject("name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); window.t_description = new GameObject("description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        window.particles = new GameObject("particles"); window.particles.transform.SetParent(windowObject.transform); window.tabsExplicitNavigation = windowObject.AddComponent<TabsExplicitNavigation>(); window.tabsExplicitNavigation.content = windowObject.transform;
        var buttonObject = new GameObject("original button", typeof(RectTransform), typeof(Button)); buttonObject.transform.SetParent(windowObject.transform); var template = buttonObject.AddComponent<EncounterButton>(); window.b_generic = buttonObject;
        var explicitNavigation = template.GetComponent<Button>().navigation; explicitNavigation.mode = Navigation.Mode.Explicit; template.GetComponent<Button>().navigation = explicitNavigation;
        template.rarityContainer = new GameObject("rarity"); template.rarityContainer.transform.SetParent(buttonObject.transform);
        template.background = buttonObject.AddComponent<Image>(); template.selectedOverlay = new GameObject("selection", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); template.selectedOverlay.transform.SetParent(buttonObject.transform);
        template.iconBorder = new GameObject("border", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); template.iconBorder.transform.SetParent(buttonObject.transform);
        template.iconBackground = new GameObject("icon background", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); template.iconBackground.transform.SetParent(buttonObject.transform);
        template.t_description = new GameObject("offer text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); template.t_description.transform.SetParent(buttonObject.transform);
        template.t_rarity = new GameObject("rarity text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); template.t_rarity.transform.SetParent(buttonObject.transform);
        typeof(EncounterWindows).GetField("activeEncounterWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiManager.encounterWindows, window);
        window.Open(EEncounter.BalanceShrine);
        var buttons = windowObject.GetComponentsInChildren<EncounterButton>(true);
        Require(buttons.Length == 3 && buttons[0].t_description.text.Contains("Double ") && buttons[1].t_description.text.Contains("Halve "), "window displays both balance offers and decline button");
        var firstButton = buttons[0].GetComponent<Button>(); var middleButton = buttons[1].GetComponent<Button>(); var lastButton = buttons[2].GetComponent<Button>();
        Require(firstButton.navigation.selectOnDown == middleButton && middleButton.navigation.selectOnUp == firstButton && middleButton.navigation.selectOnDown == lastButton && lastButton.navigation.selectOnUp == middleButton && lastButton.navigation.selectOnDown == null, "encounter buttons have vertical controller links");
        Require(firstButton.FindSelectableOnDown() == middleButton && middleButton.FindSelectableOnUp() == firstButton, "Unity selectable resolves explicit controller navigation");
        buttons[1].gameObject.SetActive(false); window.tabsExplicitNavigation.Refresh();
        Require(firstButton.navigation.selectOnDown == lastButton && lastButton.navigation.selectOnUp == firstButton, "inactive offer is skipped");
        buttons[1].gameObject.SetActive(true); window.tabsExplicitNavigation.Refresh();
        var top = new GameObject("navigation top", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        var bottom = new GameObject("navigation bottom", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        var inactiveBottom = new GameObject("inactive bottom", typeof(RectTransform), typeof(Button)).GetComponent<Button>(); inactiveBottom.gameObject.SetActive(false);
        window.tabsExplicitNavigation.topButton = top; window.tabsExplicitNavigation.bottomButtons = new Selectable[] { inactiveBottom, bottom }; window.tabsExplicitNavigation.Refresh();
        Require(top.navigation.selectOnDown == firstButton && firstButton.navigation.selectOnUp == top && lastButton.navigation.selectOnDown == bottom && bottom.navigation.selectOnUp == lastButton, "top and active bottom navigation links");
        UnityEngine.Object.DestroyImmediate(top.gameObject); UnityEngine.Object.DestroyImmediate(bottom.gameObject); UnityEngine.Object.DestroyImmediate(inactiveBottom.gameObject);
        var displayed = (EncounterOffer[])typeof(EncounterUi).GetField("offers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(window);
        typeof(MyButton).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(buttons[0], null);
        buttons[0].GetComponent<Button>().onClick.Invoke();
        Require(!windowObject.activeSelf && inventory.statInventory.permanentChanges[displayed[0].effects[0].statModifier.stat].Contains(displayed[0].effects[0].statModifier), "button callback applies selected offer and closes encounter");
        window.tabsExplicitNavigation.topButton = null; window.tabsExplicitNavigation.bottomButtons = null;
        typeof(EncounterWindows).GetField("activeEncounterWindow", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(uiManager.encounterWindows, window);
        window.Open(EEncounter.BalanceShrine);
        cost.value = -1000f;
        buttons[2].SetOffer(priced, false);
        Require(!(bool)typeof(EncounterButton).GetField("canAccept", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(buttons[2]), "expensive offer is unavailable");
        buttons[2].SetDeclineOffer(2);
        Require((bool)typeof(EncounterButton).GetField("canAccept", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(buttons[2]), "reused decline button must be available");
        typeof(MyButton).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(buttons[2], null);
        buttons[2].GetComponent<Button>().onClick.Invoke();
        Require(!windowObject.activeSelf, "reused decline button closes encounter");
        balanceData.localizedName = originalEncounterName; balanceData.localizedDescription = originalEncounterDescription;
        UnityEngine.Object.DestroyImmediate(window.t_name.gameObject); UnityEngine.Object.DestroyImmediate(window.t_description.gameObject); UnityEngine.Object.DestroyImmediate(windowObject);
        Debug.Log("EncounterOffersPortCheck passed: pools, stat offers, and balance offers");
        UnityEngine.Object.DestroyImmediate(root);
    }
}
