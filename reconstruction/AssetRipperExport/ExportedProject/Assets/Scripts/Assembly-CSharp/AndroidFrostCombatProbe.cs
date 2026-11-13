#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in observation: grant Frost Walker and let its ordinary attack loop run.
internal sealed class AndroidFrostCombatProbe : MonoBehaviour
{
    readonly HashSet<Enemy> boosted = new HashSet<Enemy>();
    readonly HashSet<Enemy> hit = new HashSet<Enemy>();
    int hits, spawned;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_frost_combat_probe", false)) return;
        var root = new GameObject("frost combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidFrostCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Frost combat probe INCONCLUSIVE: gameplay unavailable"); yield break; }
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        var inventory = player.inventory.weaponInventory;
        foreach (var weapon in inventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetWeapon(EWeapon.Frostwalker);
        if (data == null) { Debug.LogError("Frost combat probe INCONCLUSIVE: weapon missing"); yield break; }
        if (!inventory.weapons.ContainsKey(EWeapon.Frostwalker)) inventory.AddWeapon(data, null);
        inventory.ToggleWeapon(EWeapon.Frostwalker, true);
        Debug.Log("Frost combat probe READY: granted Frost Walker, player protected, newly spawned enemies boosted");
        deadline = Time.realtimeSinceStartup + 100f; Enemy target = null;
        while (Time.realtimeSinceStartup < deadline && target == null)
        {
            foreach (var candidate in hit)
                if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.HasDebuff(EDebuff.Freeze)) { target = candidate; break; }
            yield return null;
        }
        if (target == null) { Debug.LogWarning($"Frost combat probe INCONCLUSIVE: spawned={spawned}, frostHits={hits}, no observed frozen target"); Unsubscribe(); yield break; }
        int id = target.GetInstanceID(); float start = MyTime.time;
        inventory.ToggleWeapon(EWeapon.Frostwalker, false);
        Debug.Log($"Frost combat probe FROZEN: target={id}, hits={hits}, remainingTicks={target.debuffs[EDebuff.Freeze].ticksLeft}; weapon disabled");
        deadline = Time.realtimeSinceStartup + 60f;
        while (target != null && target.gameObject.activeInHierarchy && target.HasDebuff(EDebuff.Freeze) && Time.realtimeSinceStartup < deadline) yield return null;
        bool active = target != null && target.gameObject.activeInHierarchy;
        bool expired = active && !target.HasDebuff(EDebuff.Freeze);
        Debug.Log($"Frost combat probe {(expired ? "PASS" : "INCONCLUSIVE")}: target={id}, spawned={spawned}, frostHits={hits}, active={active}, expired={expired}, elapsedGame={MyTime.time-start:F2}");
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if (enemy == null || enemy.IsBoss()) return; enemy.SetDiagnosticHealth(1000f); boosted.Add(enemy); spawned++; }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null || !boosted.Contains(enemy)) return;
        if (dc.damageSource != EWeapon.Frostwalker.ToString()) return;
        hits++; hit.Add(enemy); Debug.Log($"Frost combat probe HIT: target={enemy.GetInstanceID()}, damage={dc.damage:F2}, source={dc.damageSource}");
    }
    void Unsubscribe() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
    void OnDestroy() => Unsubscribe();
}
#endif
