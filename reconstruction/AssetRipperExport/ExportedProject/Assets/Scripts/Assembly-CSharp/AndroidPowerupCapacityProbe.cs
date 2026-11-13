#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in controlled spawn; the orb's movement and terrain collision remain real.
internal sealed class AndroidPowerupCapacityProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_powerup_capacity_probe", false)) return;
        var go = new GameObject("Powerup capacity probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPowerupCapacityProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null || EffectManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = PickupManager.Instance; var enemies = EnemyManager.Instance;
        if (player == null || manager == null || enemies == null || EffectManager.Instance == null) { Debug.LogError("Powerup capacity probe: scene unavailable"); yield break; }
        bool oldWaves = enemies.enabledWaves; enemies.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var count = typeof(PickupManager).GetField("numPowerupsOnMap", BindingFlags.NonPublic | BindingFlags.Instance);
        int Count() => (int)count.GetValue(manager);
        var existing = new HashSet<Pickup>(FindObjectsOfType<Pickup>());
        var oldOrbs = new HashSet<PickupOrb>(FindObjectsOfType<PickupOrb>());
        int before = Count();
        EffectManager.Instance.SpawnPickupOrb(EPickup.Haste, player.transform.position + player.transform.right * 5f + Vector3.up * 2f);
        int flight = Count();
        Debug.Log($"Powerup capacity probe: before={before}, inFlight={flight}");
        foreach (var orb in FindObjectsOfType<PickupOrb>()) if (!oldOrbs.Contains(orb))
        {
            Debug.Log($"Powerup capacity probe: orb={orb.ePickup}, particleCount={(orb.particleSystems != null ? orb.particleSystems.Length : -1)}, particle={(orb.particleSystems != null && orb.particleSystems.Length > 0 ? orb.particleSystems[0].main.startColor.color.ToString("F3") : "none")}, trailStart={(orb.trail != null ? orb.trail.startColor.ToString("F3") : "none")}, trailEnd={(orb.trail != null ? orb.trail.endColor.ToString("F3") : "none")}");
        }
        Pickup spawned = null; deadline = Time.realtimeSinceStartup + 18f;
        while (Time.realtimeSinceStartup < deadline && spawned == null)
        {
            foreach (var item in FindObjectsOfType<Pickup>()) if (!existing.Contains(item) && item.ePickup == EPickup.Haste) { spawned = item; break; }
            if (spawned == null) yield return new WaitForSecondsRealtime(.2f);
        }
        int landed = Count();
        Debug.Log($"Powerup capacity probe: naturalLanding={spawned != null}, count={landed}");
        if (spawned != null) { manager.PickupTriggered(spawned); Debug.Log($"Powerup capacity probe: forcedCollection count={Count()}"); }
        if (player != null) invincible.SetValue(player, oldInvincible);
        if (enemies != null) enemies.enabledWaves = oldWaves;
        foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
    }
}
#endif
