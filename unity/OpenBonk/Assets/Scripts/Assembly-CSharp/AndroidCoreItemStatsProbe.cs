#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Opt-in isolated gameplay diagnostic. No code runs without the intent flag.
internal sealed class AndroidCoreItemStatsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_core_item_stats_probe", false)) return;
        var go = new GameObject("core item stat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidCoreItemStatsProbe>();
    }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .0005f;
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || !PlayerStats.HasStats() || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || player.playerMovement == null || !PlayerStats.HasStats()) { Debug.LogError("Core item stats probe: player unavailable"); yield break; }
        var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var inventory = player.inventory.itemInventory;
        float evasion0 = PlayerStats.GetStat(EStat.Evasion), rawEvasion0 = PlayerStats.GetStatRaw(EStat.Evasion), xp0 = PlayerStats.GetStat(EStat.XpIncreaseMultiplier);
        var movement = PlayerStats.GetStatComponents(EStat.MoveSpeedMultiplier);
        float baseMove = movement.baseValue, additive0 = movement.additiveValue, multiplier = movement.multiplicativeValue;
        float speed0 = player.playerMovement.movementValues.GetMaxSpeed();
        inventory.AddItem(EItem.SlipperyRing, 2); inventory.AddItem(EItem.TimeBracelet, 2); inventory.AddItem(EItem.TurboSocks, 2);
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && !Near(PlayerStats.GetStatRaw(EStat.Evasion), rawEvasion0 + .3f)) yield return null;
        float evasion = PlayerStats.GetStat(EStat.Evasion), xp = PlayerStats.GetStat(EStat.XpIncreaseMultiplier);
        float rawEvasion = PlayerStats.GetStatRaw(EStat.Evasion), expectedEvasion = rawEvasion / (rawEvasion + 1f) * .999f;
        float speed = player.playerMovement.movementValues.GetMaxSpeed();
        float expectedSpeed = player.playerMovement.movementValues.GetMaxSpeedNoMultiplier() * baseMove * (additive0 + .3f) * multiplier;
        bool gained = Near(rawEvasion, rawEvasion0 + .3f) && Near(evasion, expectedEvasion) && Near(xp, xp0 + .16f) && Near(speed, expectedSpeed);
        inventory.RemoveItem(EItem.SlipperyRing, false); inventory.RemoveItem(EItem.SlipperyRing, false);
        inventory.RemoveItem(EItem.TimeBracelet, false); inventory.RemoveItem(EItem.TimeBracelet, false);
        inventory.RemoveItem(EItem.TurboSocks, false); inventory.RemoveItem(EItem.TurboSocks, false);
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && !Near(PlayerStats.GetStat(EStat.Evasion), evasion0)) yield return null;
        bool removed = Near(PlayerStats.GetStat(EStat.Evasion), evasion0) && Near(PlayerStats.GetStat(EStat.XpIncreaseMultiplier), xp0) && Near(player.playerMovement.movementValues.GetMaxSpeed(), speed0);
        Debug.Log($"Core item stats probe {(gained && removed ? "PASS" : "FAIL")}: evasionRaw={rawEvasion0:F4}->{rawEvasion:F4}, evasion={evasion0:F4}->{evasion:F4}, xpMultiplier={xp0:F4}->{xp:F4}, maxSpeed={speed0:F4}->{speed:F4}, expectedSpeed={expectedSpeed:F4}, removed={removed}");
        manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
    }
}
#endif
