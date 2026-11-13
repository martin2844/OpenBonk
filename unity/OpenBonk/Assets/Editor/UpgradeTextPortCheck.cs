using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Mouse;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public static class UpgradeTextPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception("UpgradeTextPortCheck: " + message); }
    public static void Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var statTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Stats_en.asset");
        string statName = statTable.GetEntry("DamageMultiplier_NAME").GetLocalizedString();
        string statDescription = statTable.GetEntry("DamageMultiplier_DESC").GetLocalizedString();
        Require(Tooltip.GetTooltipString(EStat.DamageMultiplier) == "<link=\"DamageMultiplier\"><color=#ffe88a>" + statName + "</color></link>", "stat tooltip uses localized name");
        Require(Tooltip.GetIdInfo("DamageMultiplier") == statDescription, "stat tooltip uses localized description");
        Require(Tooltip.GetIdInfo("unknown") == "???: unknown", "unknown tooltip ID");
        Require(Tooltip.GetTooltipString("word") == "<link=\"word\"><color=#42b9f5>word</color></link>", "default tooltip color");
        Require(Tooltip.GetTooltipString("word", null) == "<link=\"word\"><color=>word</color></link>", "null tooltip color matches original");
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        Require(StatUtility.GetModificationString(EStatModifyType.Addition, EStat.DamageMultiplier, .25f) == "25%", "additive percentage");
        Require(StatUtility.GetModificationString(EStatModifyType.Multiplication, EStat.DamageMultiplier, .25f) == "1.25x", "multiplicative display");
        Require(StatUtility.GetModificationString(EStatModifyType.Flat, EStat.Projectiles, 2f) == "+2", "flat value prefix");
        Require(StatUtility.GetModificationString(EStatModifyType.Flat, EStat.Projectiles, 0f) == "+0", "zero flat value prefix");
        Require(StatUtility.GetModificationString(EStatModifyType.Flat, EStat.CritChance, .125f) == "12.5%", "flat percentage stat");
        Require(StatUtility.GetWeaponModificationString(EStatModifyType.Flat, EStat.ProjectileSpeedMultiplier, 2f) == "2.0", "weapon speed decimal");
        Require(StatUtility.GetWeaponModificationString(EStatModifyType.Flat, EStat.DurationMultiplier, 2.25f) == "2.25s", "weapon duration unit");
        Require(StatUtility.EncapsulateNumber("+2", "red") == "<b><color=red>+2</color></b>", "formatted number color");
        Require(StatUtility.EncapsulateNumber(2.5f, "red") == "<b><color=red>2.5</color></b>", "formatted float color");
        var shrineModifier = new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = 2f };
        Require(StatUtility.GetUpgradeDescriptionStat(shrineModifier, "red") == "Gain <b><color=red>+2</color></b> " + Tooltip.GetTooltipString(EStat.DamageMultiplier), "localized shrine upgrade description");
        var root = new GameObject("test state"); root.SetActive(false); GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
        inventory.weaponInventory = new WeaponInventory(); inventory.itemInventory = new ItemInventory(); inventory.tomeInventory = new TomeInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        inventory.playerStats = stats;
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBlackHole.asset"); data.Init();
        var originalName = data.localizedName; var originalDescription = data.localizedDescription;
        data.localizedName = new LocalizedString("Weapons", "BLACKHOLE_NAME"); data.localizedDescription = new LocalizedString("Weapons", "BLACKHOLE_DESC");
        var buttonObject = new GameObject("offer", typeof(RectTransform), typeof(Button)); buttonObject.SetActive(false); var button = buttonObject.AddComponent<UpgradeButton>();
        button.t_name = new GameObject("name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        button.t_description = new GameObject("description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        button.t_level = new GameObject("level", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        button.t_rarity = new GameObject("rarity", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        button.SetUpgrade(data);
        Require(button.t_name.text == "Black Hole" && button.t_description.text == "Shoots a black hole that pulls enemies in." && button.t_level.text == "NEW", "new weapon uses original name and description");
        var weapon = new WeaponBase(data); inventory.weaponInventory.weapons.Add(data.eWeapon, weapon);
        var damageOffer = new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = 2f };
        float currentDamage = weapon.GetValue(EStat.DamageMultiplier);
        float nextDamage = weapon.GetTestUpdateStat(EStat.DamageMultiplier, damageOffer);
        string line = Tooltip.GetTooltipString(EStat.DamageMultiplier) + ": " + StatUtility.GetWeaponModificationString(damageOffer.modifyType, damageOffer.stat, currentDamage, false) + " <sprite name=arrow> <color=green>" + StatUtility.GetWeaponModificationString(damageOffer.modifyType, damageOffer.stat, nextDamage, false) + "</color>";
        Require(StatUtility.GetUpgradeDescriptionWeapon(new List<StatModifier> { damageOffer, damageOffer }, data) == line + "\n" + line, "original weapon description and line separation");
        Require(weapon.GetValue(EStat.DamageMultiplier) == currentDamage, "description preview does not apply upgrade");
        foreach (var modifier in new[] {
            new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = 2f },
            new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = .25f },
            new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Multiplication, modification = 1.5f } })
        {
            float before = weapon.GetValue(EStat.DamageMultiplier);
            float preview = weapon.GetTestUpdateStat(EStat.DamageMultiplier, modifier);
            Require(weapon.GetValue(EStat.DamageMultiplier) == before, "preview does not mutate equipped weapon");
            weapon.Upgrade(new List<StatModifier> { modifier });
            Require(Mathf.Abs(preview - weapon.GetValue(EStat.DamageMultiplier)) < .0001f, "preview matches applied modifier for " + modifier.modifyType);
        }
        button.SetUpgrade(data);
        Require(button.t_name.text == "Black Hole" && button.t_level.text == "LVL 4" && button.t_description.text.Contains("<sprite name=arrow> <color=green>"), "owned weapon displays original stat preview");
        var tome = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeDamage.asset");
        var originalTomeName = tome.localizedName; tome.localizedName = new LocalizedString("Tomes", "TOME_DAMAGE_NAME");
        string tomePrefix = Tooltip.GetTooltipString(EStat.DamageMultiplier) + ": ";
        string arrow = " <sprite name=arrow> <color=green>";
        string newTomeText = tomePrefix + "1x" + arrow + "1.08x</color>";
        button.SetUpgrade(tome);
        Require(button.t_level.text == "NEW" && button.t_description.text == newTomeText, "new tome offer previews its base modifier");
        inventory.tomeInventory.tomeLevels[tome.eTome] = 1;
        inventory.tomeInventory.tomeUpgrade[tome.eTome] = new StatModifier { stat = tome.statModifier.stat, modifyType = tome.statModifier.modifyType, modification = 1.08f };
        var tomeOffer = new StatModifier { stat = tome.statModifier.stat, modifyType = tome.statModifier.modifyType, modification = .08f };
        string ownedTomeText = tomePrefix + "1.08x" + arrow + "1.16x</color>";
        Require(StatUtility.GetUpgradeDescriptionTome(new List<StatModifier> { tomeOffer, tomeOffer }, tome) == ownedTomeText + "\n" + ownedTomeText, "owned tome uses accumulated modifier and separates lines");
        Require(StatUtility.GetUpgradeDescriptionTomeModifier(tomeOffer, null) == newTomeText, "null tome uses initial value");
        Require(inventory.tomeInventory.tomeUpgrade[tome.eTome].modification == 1.08f, "tome description does not apply upgrade");
        button.SetUpgrade(tome);
        Require(button.t_level.text == "LVL 1" && button.t_description.text.StartsWith(tomePrefix + "1.08x" + arrow), "owned tome offer shows accumulated value");
        tome.localizedName = originalTomeName;
        data.localizedName = originalName; data.localizedDescription = originalDescription;
        Debug.Log("UpgradeTextPortCheck passed: original localized weapon text and level label");
        UnityEngine.Object.DestroyImmediate(button.t_name.gameObject); UnityEngine.Object.DestroyImmediate(button.t_description.gameObject); UnityEngine.Object.DestroyImmediate(button.t_level.gameObject); UnityEngine.Object.DestroyImmediate(button.t_rarity.gameObject); UnityEngine.Object.DestroyImmediate(buttonObject); UnityEngine.Object.DestroyImmediate(root);
    }
}
