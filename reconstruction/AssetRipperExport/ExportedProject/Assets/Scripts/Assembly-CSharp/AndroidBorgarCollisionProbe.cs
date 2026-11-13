#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in controlled drop; player collision and the real pickup/effect pools remain active.
internal sealed class AndroidBorgarCollisionProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_borgar_collision_probe", false)) return;
        var go = new GameObject("Borgar collision probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidBorgarCollisionProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.borgorPool == null || PoolManager.Instance.eatPool == null)) yield return null;
        var player = MyPlayer.Instance; var enemies = EnemyManager.Instance; var pool = PoolManager.Instance;
        if (player == null || enemies == null || pool == null || pool.borgorPool == null || pool.eatPool == null) { Debug.LogError("Borgar collision probe: scene unavailable"); yield break; }
        bool oldWaves = enemies.enabledWaves; enemies.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var health = player.inventory.playerHealth; int oldHp = health.hp; health.hp = 40;
        int oldBurgers = pool.borgorPool.CountActive, oldEffects = pool.eatPool.CountActive;
        GameObject burger = pool.borgorPool.Get();
        if (burger == null || burger.GetComponent<Borgar>() == null) { Debug.LogError("Borgar collision probe: original pickup unavailable"); Restore(); yield break; }
        burger.transform.position = player.transform.position + Vector3.up * 3f;
        var rb = burger.GetComponent<Rigidbody>(); if (rb != null) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        burger.GetComponent<Borgar>().Set(10, .08f);
        Physics.SyncTransforms();
        Debug.Log($"Borgar collision probe: spawned active={pool.borgorPool.CountActive}, hp={health.hp}, burger={burger.transform.position}, player={player.transform.position}");
        int peakEffects = oldEffects; deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline && burger.activeInHierarchy)
        {
            peakEffects = Mathf.Max(peakEffects, pool.eatPool.CountActive); yield return null;
        }
        peakEffects = Mathf.Max(peakEffects, pool.eatPool.CountActive);
        bool collected = !burger.activeInHierarchy;
        int hpAfter = health.hp, burgersAfter = pool.borgorPool.CountActive;
        Debug.Log($"Borgar collision probe: contactCollected={collected}, hpAfter={hpAfter}, burgersBefore={oldBurgers}, burgersAfter={burgersAfter}, effectsBefore={oldEffects}, effectsPeak={peakEffects}");
        yield return new WaitForSecondsRealtime(2f);
        Debug.Log($"Borgar collision probe: finished effectsAfter={pool.eatPool.CountActive}, hpAfter={hpAfter}");
        Restore();
        void Restore() { if (health != null) health.hp = oldHp; if (player != null) invincible.SetValue(player, oldInvincible); if (enemies != null) enemies.enabledWaves = oldWaves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
