using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class LevelupToolsPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/PopupText_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("LevelupToolsPortCheck: " + message); }
    static MyButtonOffersUtility Button(string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Button)); obj.SetActive(false);
        var result = obj.AddComponent<MyButtonOffersUtility>();
        result.t_amount = new GameObject("amount", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        result.t_price = new GameObject("price", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        result.disbaledOverlay = new GameObject("disabled");
        return result;
    }
    public static void Run()
    {
        var root = new GameObject("levelup test"); root.SetActive(false);
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var alwaysUi = root.AddComponent<AlwaysUi>(); AlwaysUi.Instance = alwaysUi;
        var popup = root.AddComponent<UiTextPopup>(); alwaysUi.UiTextPopup = popup;
        popup.parent = new GameObject("popup content", typeof(RectTransform)); popup.parent.SetActive(false);
        popup.t_text = new GameObject("popup text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        popup.sfx = root.AddComponent<RandomSfx>();
        GameManager.Instance = root.AddComponent<GameManager>();
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var inventory = player.inventory; inventory.skips = 2; inventory.refreshes = 2;
        var screen = root.AddComponent<LevelupScreen>(); var picker = root.AddComponent<UpgradePicker>(); screen.upgradePicker = picker; picker.buttons = Array.Empty<UpgradeButton>();
        screen.b_skip = Button("skip"); screen.b_refresh = Button("refresh"); screen.window = new GameObject("window");
        var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui; ui.encounterWindows = root.AddComponent<EncounterWindows>();
        typeof(LevelupScreen).GetField("encounterType", P).SetValue(screen, EEncounter.RandomStats);
        typeof(LevelupScreen).GetMethod("RefreshUtilityButtons", P).Invoke(screen, null);
        Require(screen.b_skip.gameObject.activeSelf && screen.b_refresh.gameObject.activeSelf, "available tools are visible");
        Require(screen.b_refresh.t_amount.text == "2" && screen.b_refresh.t_price.text == "0", "free first refresh displayed");
        screen.Refresh();
        Require(inventory.refreshes == 1 && inventory.refreshesUsed == 1 && inventory.gold == 0f, "first refresh is free and consumes a charge");
        Require(screen.b_refresh.t_price.text == "5" && screen.b_refresh.disbaledOverlay.activeSelf, "next refresh price and affordability displayed");
        screen.Refresh(); Require(inventory.refreshes == 1 && inventory.refreshesUsed == 1, "unaffordable refresh rejected");
        typeof(PlayerInventory).GetField("<gold>k__BackingField", P).SetValue(inventory, 10f);
        typeof(PlayerInventory).GetField("<goldInt>k__BackingField", P).SetValue(inventory, 10);
        screen.Refresh(); Require(inventory.refreshes == 0 && inventory.refreshesUsed == 2 && inventory.gold == 5f, "paid refresh deducts gold and consumes charge");
        screen.Refresh(); Require(inventory.refreshes == 0 && inventory.gold == 5f, "exhausted refresh rejected");
        typeof(EncounterWindows).GetField("activeEncounterWindow", P).SetValue(ui.encounterWindows, screen);
        screen.Skip(); Require(inventory.skips == 1 && inventory.skipsUsed == 1 && inventory.gold == 5f && !screen.window.activeSelf, "free skip consumes charge and closes reward");
        inventory.banishes = 1; inventory.weaponInventory = new WeaponInventory();
        screen.b_banish = Button("banish"); picker.banisModeOverlay = new GameObject("banish overlay"); picker.banisModeOverlay.SetActive(false);
        var offerObject = new GameObject("weapon offer", typeof(RectTransform), typeof(Button)); var offerButton = offerObject.AddComponent<UpgradeButton>();
        picker.buttons = new[] { offerButton }; picker.levelupScreen = screen;
        var weapon = ScriptableObject.CreateInstance<WeaponData>(); weapon.eWeapon = EWeapon.BlackHole;
        RunUnlockables.banishedUpgradables = new HashSet<UnlockableBase>();
        typeof(LevelupScreen).GetMethod("RefreshUtilityButtons", P).Invoke(screen, null);
        Require(screen.b_banish.gameObject.activeSelf && screen.b_banish.t_amount.text == "1", "available banish displayed");
        var ownedWeapon = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBlackHole.asset"); ownedWeapon.Init();
        inventory.weaponInventory.weapons.Add(ownedWeapon.eWeapon, new WeaponBase(ownedWeapon));
        screen.StartBanish(); picker.SelectUpgrade(weapon, null, offerButton, ERarity.New);
        Require(!picker.banishMode && inventory.banishes == 1 && !RunUnlockables.banishedUpgradables.Contains(weapon), "owned weapon cannot be banished or consume charge");
        Require(popup.parent.activeSelf && popup.t_text.text == "Can't banish owned item" && popup.t_text.color == Color.white, "owned banish shows the original localized popup");
        inventory.weaponInventory.weapons.Clear();
        screen.StartBanish(); Require(picker.banishMode && picker.banisModeOverlay.activeSelf, "banish mode opens");
        screen.window.SetActive(true); typeof(EncounterWindows).GetField("activeEncounterWindow", P).SetValue(ui.encounterWindows, screen);
        picker.SelectUpgrade(weapon, null, offerButton, ERarity.New);
        Require(RunUnlockables.banishedUpgradables.Contains(weapon) && inventory.banishes == 0 && inventory.banishesUsed == 1, "banish excludes unowned weapon and consumes charge");
        Require(!screen.window.activeSelf && !picker.banishMode && !offerObject.activeSelf, "banish closes reward and clears mode");
        screen.StartBanish(); Require(!picker.banishMode, "exhausted banish rejected");
        var item = ScriptableObject.CreateInstance<ItemData>();
        RunUnlockables.banishedItems = new HashSet<ItemData>();
        RunUnlockables.availableItems = new Dictionary<EItemRarity, List<ItemData>> { [item.rarity] = new List<ItemData> { item } };
        inventory.banishes = 1; inventory.banishesUsed = 0; screen.window.SetActive(true);
        typeof(EncounterWindows).GetField("activeEncounterWindow", P).SetValue(ui.encounterWindows, screen);
        screen.StartBanish(); picker.SelectItem(item);
        Require(RunUnlockables.banishedItems.Contains(item) && !RunUnlockables.availableItems[item.rarity].Contains(item) && inventory.banishes == 0 && !screen.window.activeSelf, "item banish removes item from future pool");
        Debug.Log("LevelupToolsPortCheck passed: counts, prices, affordability, refresh, skip, owned-weapon rejection and weapon/item banish");
        UnityEngine.Object.DestroyImmediate(item);
        UnityEngine.Object.DestroyImmediate(weapon); UnityEngine.Object.DestroyImmediate(offerObject); UnityEngine.Object.DestroyImmediate(picker.banisModeOverlay);
        UnityEngine.Object.DestroyImmediate(screen.b_banish.t_amount.gameObject); UnityEngine.Object.DestroyImmediate(screen.b_banish.t_price.gameObject); UnityEngine.Object.DestroyImmediate(screen.b_banish.disbaledOverlay); UnityEngine.Object.DestroyImmediate(screen.b_banish.gameObject);
        UnityEngine.Object.DestroyImmediate(screen.b_skip.t_amount.gameObject); UnityEngine.Object.DestroyImmediate(screen.b_skip.t_price.gameObject); UnityEngine.Object.DestroyImmediate(screen.b_skip.disbaledOverlay); UnityEngine.Object.DestroyImmediate(screen.b_skip.gameObject);
        UnityEngine.Object.DestroyImmediate(screen.b_refresh.t_amount.gameObject); UnityEngine.Object.DestroyImmediate(screen.b_refresh.t_price.gameObject); UnityEngine.Object.DestroyImmediate(screen.b_refresh.disbaledOverlay); UnityEngine.Object.DestroyImmediate(screen.b_refresh.gameObject);
        UnityEngine.Object.DestroyImmediate(popup.t_text.gameObject); UnityEngine.Object.DestroyImmediate(popup.parent); UnityEngine.Object.DestroyImmediate(screen.window); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; UiManager.Instance = null; GameManager.Instance = null; AlwaysUi.Instance = null;
    }
}
