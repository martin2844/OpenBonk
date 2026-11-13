using System;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class ShopProgressionCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("ShopProgressionCheck: " + message); }
    public static void Run()
    {
        var go = new GameObject("shop fixture"); go.SetActive(false);
        var save = go.AddComponent<SaveManager>(); save.stats = new StatsSaveFile(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        foreach (EShopItem item in Enum.GetValues(typeof(EShopItem))) save.progression.shopItems[item] = 0;
        var refresh = AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Refresh.asset");
        var weapon = AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Weapons.asset");
        var tome = AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Tomes.asset");
        Check(refresh != null && weapon != null && tome != null && refresh.GetIcon() != null && refresh.GetUnlockRequirement() != null && refresh.GetInternalName() == "Refresh", "exported shop asset data");
        Check(refresh.GetPrice() == refresh.price && refresh.GetLevel() == 0 && !refresh.CanBuy(), "first price and insufficient silver");
        Check(weapon.GetMaxLevel() == 0 && tome.GetMaxLevel() == 0, "slot limits initially locked");
        save.progression.achievements.Add("a_weaponSlots"); save.progression.achievements.Add("a_tomeSlots2");
        Check(weapon.GetMaxLevel() == 1 && tome.GetMaxLevel() == 1, "slot limits follow separate achievements");
        save.progression.achievements.Add("a_weaponSlots2"); save.progression.achievements.Add("a_tomeSlots");
        Check(weapon.GetMaxLevel() == 2 && tome.GetMaxLevel() == 2, "both slot limits");
        int change = 0; ProgressionSaveFile.A_SilverChanged += amount => change += amount;
        save.progression.silver = 100000; int initial = save.progression.silver; int price0 = refresh.GetPrice();
        Check(save.progression.PurchaseShopItem(refresh) && save.progression.GetShopItemLevel(EShopItem.Refresh) == 1 && save.progression.HasShopItem(EShopItem.Refresh) && save.progression.silver == initial - price0 && change == -price0,"first purchase");
        int price1 = Mathf.RoundToInt(refresh.price * refresh.exponentialMultiplier) + refresh.linearIncrease;
        Check(refresh.GetPrice() == price1 && refresh.GetRefundPrice() == price0, "level pricing");
        Check(save.progression.PurchaseShopItem(refresh) && refresh.GetLevel() == 2 && refresh.GetRefundPrice() == price1,"second purchase");
        Check(save.progression.RefundShopItem(refresh) && refresh.GetLevel() == 1 && save.progression.silver == initial - price0,"refund last level");
        Check(save.progression.RefundShopItem(refresh) && refresh.GetLevel() == 0 && save.progression.silver == initial && !save.progression.HasShopItem(EShopItem.Refresh) && !save.progression.RefundShopItem(refresh),"full refund and no over-refund");
        save.progression.silver = int.MaxValue / 2;
        for (int i = 0; i < refresh.GetMaxLevel(); i++) Check(save.progression.PurchaseShopItem(refresh), "purchase level " + i);
        Check(refresh.IsMaxLevel() && !save.progression.PurchaseShopItem(refresh), "max level gate");
        Check(weapon.CompareTo(refresh) < 0 && refresh.CompareTo(weapon) > 0,"shop sorting order");
        Debug.Log("ShopProgressionCheck PASSED: original assets, prices, purchases, refunds and slot achievements");
        UnityEngine.Object.DestroyImmediate(go);
    }
}
