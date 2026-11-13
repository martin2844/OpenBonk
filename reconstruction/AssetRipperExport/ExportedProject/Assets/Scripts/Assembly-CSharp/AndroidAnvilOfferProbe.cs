#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Disposable run: grant Anvil and apply one controlled weapon offer.
internal sealed class AndroidAnvilOfferProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_anvil_offer_probe", false)) return;
        var go = new GameObject("Anvil offer probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidAnvilOfferProbe>();
    }
    IEnumerator Start() {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || !player.inventory.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out var weapon)) { Debug.LogError("Anvil offer probe INCONCLUSIVE: player or weapon unavailable"); yield break; }
        var inventory = player.inventory.itemInventory;
        if (inventory.GetAmount(EItem.Anvil) != 0 || weapon.weaponData.upgradeData.upgradeModifiers.Count < 3) { Debug.LogError("Anvil offer probe INCONCLUSIVE: unexpected inventory or upgrade data"); yield break; }
        var before = weapon.weaponData.GetUpgradeOffer(ERarity.Rare);
        inventory.AddItem(EItem.Anvil);
        var offer = weapon.weaponData.GetUpgradeOffer(ERarity.Rare);
        var unique = new HashSet<EStat>(); var expected = new List<float>();
        foreach (var modifier in offer) { unique.Add(modifier.stat); expected.Add(weapon.GetTestUpdateStat(modifier.stat, modifier)); }
        int level = weapon.level;
        player.inventory.weaponInventory.AddWeapon(weapon.weaponData, offer);
        bool valuesMatch = true;
        for (int i = 0; i < offer.Count; i++) valuesMatch &= Mathf.Abs(expected[i] - weapon.GetValue(offer[i].stat)) < .0001f;
        bool ok = before.Count == 2 && inventory.GetAmount(EItem.Anvil) == 1 && offer.Count == 3 && unique.Count == 3 && weapon.level == level + 1 && valuesMatch;
        Debug.Log($"Anvil offer probe {(ok ? "PASS" : "FAIL")}: before={before.Count}, anvils={inventory.GetAmount(EItem.Anvil)}, after={offer.Count}, unique={unique.Count}, level={level}->{weapon.level}, valuesMatch={valuesMatch}");
    }
}
#endif
