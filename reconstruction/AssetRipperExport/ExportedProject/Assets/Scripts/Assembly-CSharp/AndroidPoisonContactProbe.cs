#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in controlled setup; scorpion movement, collision and damage remain active.
internal sealed class AndroidPoisonContactProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_poison_contact_probe", false)) return;
        var go = new GameObject("Poison contact probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPoisonContactProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || manager == null || DataManager.Instance == null) { Debug.LogError("Poison contact probe: game unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, false);
        player.inventory.playerHealth.hp = 10000; player.inventory.playerHealth.maxHp = 10000;
        var data = DataManager.Instance.GetEnemyData(EEnemy.Scorpion);
        if (data == null) { Debug.LogError("Poison contact probe: scorpion data unavailable"); Restore(); yield break; }
        var enemy = manager.SpawnEnemy(data, player.transform.position + player.transform.forward * 4f, -1, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Poison contact probe: spawn failed"); Restore(); yield break; }
        int hits = 0; PlayerHealth.A_TakeDamage += OnDamage;
        Debug.Log($"Poison contact probe: spawned={data.enemyName}, isPoison={data.isPoison}, hp={player.inventory.playerHealth.hp}");
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && player != null && enemy != null && enemy.gameObject.activeInHierarchy && hits == 0) yield return new WaitForSecondsRealtime(.25f);
        Debug.Log($"Poison contact probe: finished hits={hits}, poisoned={(player != null && player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Poison))}");
        PlayerHealth.A_TakeDamage -= OnDamage; Restore();
        void OnDamage(PlayerHealth health, DamageContainer dc, bool shield)
        {
            if (health != player.inventory.playerHealth || dc.enemy != enemy) return;
            hits++;
            Debug.Log($"Poison contact probe: physical hit attacker={enemy.enemyData.enemyName}, source={dc.damageSource}, damage={dc.damage:F2}, poisoned={player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Poison)}");
        }
        void Restore() { if (player != null) invincible.SetValue(player, oldInvincible); if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
