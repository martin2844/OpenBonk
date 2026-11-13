#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;
using UnityEngine.EventSystems;

// Disposable run. XP pickup opens the real screen; the offered weapon is forced.
internal sealed class AndroidAnvilOfferUiProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_anvil_offer_ui_probe", false)) return;
        var go = new GameObject("Anvil offer UI probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidAnvilOfferUiProbe>();
    }
    IEnumerator Start() {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null || PickupManager.Instance == null || !player.inventory.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out var weapon)) { Debug.LogError("Anvil offer UI probe INCONCLUSIVE: game unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        player.inventory.itemInventory.AddItem(EItem.Anvil);
        var xp = player.inventory.playerXp; xp.xp = 6; xp.level = 0;
        typeof(PlayerXp).GetField("leftOverXp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(xp, 0f);
        var pickup = PickupManager.Instance.SpawnPickup(EPickup.Xp, player.transform.position, 1, false, 0f);
        deadline = Time.realtimeSinceStartup + 40f; LevelupScreen screen = null;
        while (Time.realtimeSinceStartup < deadline) {
            screen = FindObjectOfType<LevelupScreen>();
            if (screen != null && screen.window != null && screen.window.activeInHierarchy && screen.upgradePicker != null && screen.upgradePicker.GetNumUpgrades() > 0) break;
            yield return null;
        }
        if (screen == null || screen.upgradePicker == null || screen.window == null || !screen.window.activeInHierarchy || screen.upgradePicker.GetNumUpgrades() == 0) { Debug.LogError("Anvil offer UI probe INCONCLUSIVE: level-up screen did not open"); yield break; }
        var button = screen.upgradePicker.buttons[0]; button.SetUpgrade(weapon.weaponData);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var offer = (List<StatModifier>)typeof(UpgradeButton).GetField("upgradeOffer", flags).GetValue(button);
        var rarity = (ERarity)typeof(UpgradeButton).GetField("rarity", flags).GetValue(button);
        var unique = new HashSet<EStat>(); var expected = new List<float>();
        foreach (var modifier in offer) { unique.Add(modifier.stat); expected.Add(weapon.GetTestUpdateStat(modifier.stat, modifier)); }
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && !button.GetButton().interactable) yield return null;
        if (!button.GetButton().interactable || EventSystem.current == null) { Debug.LogError("Anvil offer UI probe INCONCLUSIVE: button unavailable"); yield break; }
        EventSystem.current.SetSelectedGameObject(button.gameObject);
        int oldLevel = weapon.level;
        Debug.Log($"Anvil offer UI probe READY: rarity={rarity}, count={offer.Count}, unique={unique.Count}, description={button.t_description.text.Replace('\n','|')}");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && weapon.level == oldLevel) yield return null;
        yield return null;
        bool values = true; for (int i=0;i<offer.Count;i++) values &= Mathf.Abs(weapon.GetValue(offer[i].stat) - expected[i]) < .0001f;
        bool count = rarity == ERarity.Common ? offer.Count == 2 || offer.Count == 3 : offer.Count == 3;
        bool pass = player.inventory.itemInventory.GetAmount(EItem.Anvil) == 1 && pickup != null && !pickup.gameObject.activeInHierarchy && count && unique.Count == offer.Count && weapon.level == oldLevel + 1 && values && !screen.window.activeInHierarchy;
        Debug.Log($"Anvil offer UI probe {(pass ? "PASS" : "FAIL")}: rarity={rarity}, count={offer.Count}, unique={unique.Count}, level={oldLevel}->{weapon.level}, statsMatch={values}, windowClosed={!screen.window.activeInHierarchy}, pickupCollected={(pickup != null && !pickup.gameObject.activeInHierarchy)}");
    }
}
#endif
