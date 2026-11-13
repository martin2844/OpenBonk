#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in disposable run: inject stacks, leave normal waves and Firestaff active.
internal sealed class AndroidLightningOrbCombatProbe : MonoBehaviour
{
    readonly HashSet<Enemy> boosted = new HashSet<Enemy>();
    readonly HashSet<Enemy> struck = new HashSet<Enemy>();
    int spawned, fireHits, lightningHits, wrongElement;
    float lightningDamage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_orb_combat_probe", false)) return;
        var root = new GameObject("lightning orb combat probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidLightningOrbCombatProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Lightning orb probe INCONCLUSIVE: gameplay unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.LightningOrb, 4);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn;
        Enemy.A_Damage += OnDamage;
        Debug.Log("Lightning orb probe READY: four items, protected player, boosted enemies");
        deadline = Time.realtimeSinceStartup + 90f;
        Enemy stunned = null;
        while (Time.realtimeSinceStartup < deadline && stunned == null)
        {
            foreach (var enemy in struck)
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.debuffs.ContainsKey(EDebuff.Stun)) { stunned = enemy; break; }
            yield return null;
        }
        bool pass = spawned > 0 && fireHits > 0 && lightningHits > 0 && lightningDamage > 0 && wrongElement == 0 && stunned != null;
        Debug.Log($"Lightning orb probe {(pass ? "PASS" : "INCONCLUSIVE")}: spawned={spawned} fireHits={fireHits} lightningHits={lightningHits} lightningDamage={lightningDamage:F2} wrongElement={wrongElement} sameEnemyStunned={(stunned != null ? stunned.GetInstanceID() : 0)}");
        Enemy.A_EnemySpawned -= OnSpawn;
        Enemy.A_Damage -= OnDamage;
    }

    void OnSpawn(Enemy enemy) { if (enemy == null || enemy.IsBoss()) return; enemy.SetDiagnosticHealth(1000f); boosted.Add(enemy); spawned++; }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null || !boosted.Contains(enemy)) return;
        if (dc.damageSource == "FireStaff") fireHits++;
        if (dc.damageSource != "LightningOrb") return;
        struck.Add(enemy); lightningHits++; lightningDamage += dc.damage;
        if (dc.element != EElement.Lightning) wrongElement++;
        Debug.Log($"Lightning orb probe HIT: enemy={enemy.GetInstanceID()} damage={dc.damage:F2} element={dc.element}");
    }
    void OnDestroy() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
}
#endif
