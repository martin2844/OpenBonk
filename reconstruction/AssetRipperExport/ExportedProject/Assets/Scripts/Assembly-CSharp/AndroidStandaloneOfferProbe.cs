#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidStandaloneOfferProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_standalone_offer_probe", false)) return;
        var go = new GameObject("Standalone offer probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidStandaloneOfferProbe>();
    }
    IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EWeapon)OpenBonkContentIds.Weapon("openbonk-standalone-test", "spark");
        WeaponData data;
        try { data = DataManager.Instance.GetWeapon(id); } catch { Debug.LogError("Standalone offer INCONCLUSIVE: weapon missing"); yield break; }
        if (player == null || player.inventory == null || !(data is OpenBonkLuaWeaponData original) || EnemyManager.Instance == null) { Debug.LogError("Standalone offer INCONCLUSIVE: run unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var w in player.inventory.weaponInventory.weapons.Values) w.Disable();
        var inv = player.inventory; bool sampledNew = false;
        for (int i = 0; i < 100; i++) if (InventoryUtility.GetRandomUpgrades().Contains(data)) sampledNew = true;
        inv.playerXp.AddXp(XpUtility.LevelToXp(1));
        LevelupScreen screen = null; end = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < end) { screen = FindObjectOfType<LevelupScreen>(); if (screen != null && screen.window.activeInHierarchy && screen.upgradePicker.GetNumUpgrades() > 0) break; yield return null; }
        if (screen == null || !screen.window.activeInHierarchy || screen.upgradePicker.GetNumUpgrades() == 0) { Debug.LogError("Standalone offer INCONCLUSIVE: level screen absent"); yield break; }
        var button = screen.upgradePicker.buttons[0]; button.SetUpgrade(data);
        bool newDisplay = button.t_name.text == data.GetName() && button.icon.texture == data.icon && button.t_level.text == "NEW";
        end = Time.realtimeSinceStartup + 10f; while (Time.realtimeSinceStartup < end && !button.GetButton().interactable) yield return null;
        button.SelectUpgrade(); yield return null;
        bool acquired = inv.weaponInventory.weapons.TryGetValue(id, out var weapon) && weapon.level == 1 && !screen.window.activeInHierarchy;
        if (!acquired) { Debug.LogError("Standalone offer FAIL: acquisition failed"); yield break; }
        weapon.Disable();
        var extra = Instantiate(original); extra.externalId = "openbonk-extra:spark";
        DataManager.Instance.RegisterExternalWeapon("openbonk-extra", "spark", extra);
        foreach (var candidate in DataManager.Instance.GetAllWeapons())
        {
            if (inv.weaponInventory.GetNumWeapons() >= InventoryUtility.GetNumAvailableWeaponSlots()) break;
            if (candidate != null && !(candidate is OpenBonkLuaWeaponData) && !inv.weaponInventory.weapons.ContainsKey(candidate.eWeapon)) { inv.weaponInventory.AddWeapon(candidate, null); inv.weaponInventory.weapons[candidate.eWeapon].Disable(); }
        }
        bool full = inv.weaponInventory.GetNumWeapons() >= InventoryUtility.GetNumAvailableWeaponSlots();
        var canUnlock = typeof(InventoryUtility).GetMethod("CanUnlockItem", BindingFlags.Static | BindingFlags.NonPublic);
        bool rejectedWhenFull = full && !(bool)canUnlock.Invoke(null, new object[] { extra });
        bool leaked = false, ownedOffered = false;
        for (int i = 0; i < 100; i++) { var offers = InventoryUtility.GetRandomUpgrades(); if (offers.Contains(extra)) leaked = true; if (offers.Contains(data)) ownedOffered = true; }
        yield return new WaitForSecondsRealtime(1f);
        inv.playerXp.AddXp(XpUtility.LevelToXp(2) - inv.playerXp.xp);
        end = Time.realtimeSinceStartup + 30f; while (Time.realtimeSinceStartup < end && !screen.window.activeInHierarchy) yield return null;
        if (!screen.window.activeInHierarchy) { Debug.LogError("Standalone offer INCONCLUSIVE: upgrade screen absent"); yield break; }
        button = screen.upgradePicker.buttons[0]; button.SetUpgrade(data);
        bool upgradeDisplay = button.t_level.text == "LVL 1" && button.t_name.text == data.GetName() && button.icon.texture == data.icon;
        var modifiers = (List<StatModifier>)typeof(UpgradeButton).GetField("upgradeOffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(button);
        var expected = new List<float>(); foreach (var modifier in modifiers) expected.Add(weapon.GetTestUpdateStat(modifier.stat, modifier));
        end = Time.realtimeSinceStartup + 10f; while (Time.realtimeSinceStartup < end && !button.GetButton().interactable) yield return null;
        button.SelectUpgrade(); yield return null;
        bool stats = modifiers.Count > 0; for (int i = 0; i < modifiers.Count; i++) stats &= Mathf.Abs(weapon.GetValue(modifiers[i].stat) - expected[i]) < .001f;
        bool upgraded = weapon.level == 2 && stats && !screen.window.activeInHierarchy;
        bool pass = sampledNew && newDisplay && acquired && rejectedWhenFull && !leaked && ownedOffered && upgradeDisplay && upgraded;
        Debug.Log($"Standalone offer {(pass ? "PASS" : "FAIL")}: sampledNew={sampledNew} newDisplay={newDisplay} acquired={acquired} full={full} rejected={rejectedWhenFull} leaked={leaked} ownedOffered={ownedOffered} upgradeDisplay={upgradeDisplay} upgraded={upgraded}");
    }
}
#endif
