#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable diagnostic: grant items and raise enemy health, leave waves and attacks running.
internal sealed class AndroidIceCubeCombatProbe : MonoBehaviour
{
    readonly HashSet<Enemy> boosted = new HashSet<Enemy>();
    readonly HashSet<Enemy> hitByIce = new HashSet<Enemy>();
    int spawned, weaponHits, iceHits, freezeEvents;
    float iceDamage;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_icecube_combat_probe", false)) return;
        var root = new GameObject("ice cube combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidIceCubeCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Ice Cube combat probe INCONCLUSIVE: gameplay unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.IceCube, 4);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage; ItemIceCube.A_FreezeEnemy += OnFreeze;
        Debug.Log("Ice Cube combat probe READY: four items, protected player, boosted enemies");
        deadline = Time.realtimeSinceStartup + 90f;
        Enemy frozen = null;
        while (Time.realtimeSinceStartup < deadline && frozen == null)
        {
            foreach (var enemy in hitByIce)
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.debuffs.ContainsKey(EDebuff.Freeze)) { frozen = enemy; break; }
            yield return null;
        }
        bool pass = spawned > 0 && weaponHits > 0 && iceHits > 0 && iceDamage > 0f && freezeEvents > 0 && frozen != null;
        Debug.Log($"Ice Cube combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: spawned={spawned} weaponHits={weaponHits} iceHits={iceHits} iceDamage={iceDamage:F2} freezeEvents={freezeEvents} sameEnemyFrozen={(frozen != null ? frozen.GetInstanceID() : 0)}");
        Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; ItemIceCube.A_FreezeEnemy -= OnFreeze;
    }
    void OnSpawn(Enemy enemy) { if (enemy == null || enemy.IsBoss()) return; enemy.SetDiagnosticHealth(1000f); boosted.Add(enemy); spawned++; }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null || !boosted.Contains(enemy)) return;
        if (dc.damageSource == "FireStaff") weaponHits++;
        if (dc.damageSource != "IceCube") return;
        hitByIce.Add(enemy); iceHits++; iceDamage += dc.damage;
        Debug.Log($"Ice Cube combat probe ICE: enemy={enemy.GetInstanceID()} damage={dc.damage:F2} element={dc.element}");
    }
    void OnFreeze() { freezeEvents++; }
    void OnDestroy() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; ItemIceCube.A_FreezeEnemy -= OnFreeze; }
}
#endif
