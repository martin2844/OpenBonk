#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Forced inventory state in a disposable test run; disabled unless requested.
internal sealed class AndroidExtraUpgradeLevelsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_extra_upgrade_levels_probe", false)) return;
        var go = new GameObject("extra upgrade levels probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExtraUpgradeLevelsProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || !player.inventory.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out var weapon)) { Debug.LogError("Extra upgrade levels probe FAIL: player or Firestaff unavailable"); yield break; }
        var manager = EnemyManager.Instance; manager.enabledWaves = false; weapon.Disable();
        var inv = player.inventory.itemInventory; var tomes = player.inventory.tomeInventory;
        var tome = DataManager.Instance.GetTome(ETome.Damage);
        if (tome == null || inv.GetAmount(EItem.Pot) != 0 || inv.GetAmount(EItem.WizardsHat) != 0 || tomes.tomeLevels.ContainsKey(ETome.Damage)) { Debug.LogError("Extra upgrade levels probe FAIL: unexpected starting inventory"); yield break; }
        bool ok = weapon.weaponData.GetMaxLevel() == 40 && tome.GetMaxLevel() == 99;
        inv.AddItem(EItem.Pot, 2); inv.AddItem(EItem.WizardsHat, 2);
        ok &= weapon.weaponData.GetMaxLevel() == 59 && tome.GetMaxLevel() == 118;
        weapon.level = 40; tomes.tomeLevels[ETome.Damage] = 99;
        player.inventory.weaponInventory.AddWeapon(weapon.weaponData, new List<StatModifier>());
        tomes.AddTome(tome, new List<StatModifier>(), ERarity.Common);
        ok &= weapon.level == 41 && tomes.GetTomeLevel(ETome.Damage) == 100;
        inv.RemoveItem(EItem.Pot, false); inv.RemoveItem(EItem.WizardsHat, false);
        int weaponPartial = weapon.weaponData.GetMaxLevel(), tomePartial = tome.GetMaxLevel();
        ok &= weaponPartial == 69 && tomePartial == 128;
        inv.RemoveItem(EItem.Pot, false); inv.RemoveItem(EItem.WizardsHat, false);
        ok &= weapon.weaponData.GetMaxLevel() == 40 && tome.GetMaxLevel() == 99 && weapon.level == 41 && tomes.GetTomeLevel(ETome.Damage) == 100;
        inv.AddItem(EItem.Pot); inv.AddItem(EItem.WizardsHat);
        player.inventory.weaponInventory.AddWeapon(weapon.weaponData, new List<StatModifier>());
        tomes.AddTome(tome, new List<StatModifier>(), ERarity.Common);
        ok &= weapon.weaponData.GetMaxLevel() == 50 && tome.GetMaxLevel() == 109 && weapon.level == 42 && tomes.GetTomeLevel(ETome.Damage) == 101;
        Debug.Log($"Extra upgrade levels probe {(ok ? "PASS" : "FAIL")}: twoStackCaps=59/118 partialCaps={weaponPartial}/{tomePartial} finalCaps=40/99 reacquiredCaps={weapon.weaponData.GetMaxLevel()}/{tome.GetMaxLevel()} levels={weapon.level}/{tomes.GetTomeLevel(ETome.Damage)}");
    }
}
#endif
