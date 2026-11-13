#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Runs only in an explicitly selected disposable Android test session.
internal sealed class AndroidEntRootProbe : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    int hits;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_ent_root_probe", false)) return;
        var go = new GameObject("Ent root probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidEntRootProbe>();
    }
    void OnDamage(PlayerHealth hp, DamageContainer dc, bool shield) { if (dc.damageSource == "Enemy") hits++; }
    IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < until && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Ent root probe INCONCLUSIVE: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetEnemyData(EEnemy.Ent1);
        var config = data == null ? null : data.specialAttacks.FirstOrDefault(a => a.attackName == "RootAttackContinous");
        var ent = data == null ? null : manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 8f, -1, true, canBeElite:false);
        if (ent == null || config == null) { Debug.LogError("Ent root probe INCONCLUSIVE: Ent/config unavailable"); yield break; }
        until = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < until && ent != null && ent.IsTeleporting()) yield return null;
        if (ent == null || ent.IsTeleporting()) { Debug.LogError("Ent root probe INCONCLUSIVE: teleport incomplete"); yield break; }
        var controller = typeof(Enemy).GetField("specialAttackController", Private).GetValue(ent) as SpecialAttackController;
        if (controller == null) { Debug.LogError("Ent root probe INCONCLUSIVE: controller unavailable"); yield break; }
        var health = player.inventory.playerHealth; var body = player.playerMovement.GetRb();
        typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, false);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", Private).SetValue(player, 0f);
        health.hp = health.maxHp = 10000; health.shield = 0f;
        // Exclude enemy contact damage so any Enemy damage comes from the forced attack.
        foreach (var existing in FindObjectsOfType<Enemy>())
        {
            if (existing != ent) { existing.gameObject.SetActive(false); continue; }
            foreach (var collider in existing.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        Vector3 initialPosition = player.transform.position;
        bool ground = Physics.Raycast(initialPosition + Vector3.up * 99f, Vector3.down, out RaycastHit groundHit, 999f, GameManager.Instance.whatIsGround);
        player.playerMovement.enabled = false;
        if (ground) { body.position = groundHit.point + Vector3.up; player.transform.position = groundHit.point + Vector3.up; }
        body.velocity = Vector3.zero; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        body.constraints = RigidbodyConstraints.FreezeAll; Physics.SyncTransforms();
        Vector3 playerPosition = body.position;
        int initialHp = health.hp; PlayerHealth.A_TakeDamage += OnDamage;
        typeof(SpecialAttackController).GetMethod("UseSpecialAttack", Private).Invoke(controller, new object[] { config });
        if (!PoolManager.Instance.enemyAttacksPools.TryGetValue(config.attackName, out var pool) || pool.CountActive == 0) { Debug.LogError("Ent root probe FAIL: attack did not start"); PlayerHealth.A_TakeDamage -= OnDamage; yield break; }
        int peakWarnings = 0;
        until = Time.realtimeSinceStartup + 100f;
        while (Time.realtimeSinceStartup < until && pool.CountActive > 0) { peakWarnings = Mathf.Max(peakWarnings, PoolManager.Instance.warningSpherePool.CountActive); yield return null; }
        bool positioned = ground && Vector3.Distance(playerPosition, groundHit.point + Vector3.up) < .2f;
        bool success = positioned && peakWarnings > 0 && hits > 0 && health.hp < initialHp && pool.CountActive == 0;
        Debug.Log($"Ent root probe {(success ? "PASS" : "FAIL")}: ground={ground}, groundPoint={groundHit.point}, player={playerPosition}, groundMask={GameManager.Instance.whatIsGround.value}, warningsPeak={peakWarnings}, hits={hits}, hp={initialHp}->{health.hp}, poolActive={pool.CountActive}");
        PlayerHealth.A_TakeDamage -= OnDamage;
    }
    void OnDestroy() { PlayerHealth.A_TakeDamage -= OnDamage; }
}
#endif
