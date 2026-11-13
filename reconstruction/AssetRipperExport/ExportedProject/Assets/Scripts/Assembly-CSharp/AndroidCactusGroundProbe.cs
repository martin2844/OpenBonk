#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Explicitly opt-in test of the original cactus projectile's ground target.
internal sealed class AndroidCactusGroundProbe : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_cactus_ground_probe", false)) return;
        var go = new GameObject("Cactus ground probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidCactusGroundProbe>();
    }
    IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < until && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Cactus ground probe INCONCLUSIVE: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetEnemyData(EEnemy.CactusShooter);
        var config = data == null ? null : data.specialAttacks.FirstOrDefault(a => a.attackName == "CactusProjectile");
        var enemy = data == null ? null : manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 8f, -1, true, canBeElite:false);
        if (enemy == null || config == null) { Debug.LogError("Cactus ground probe INCONCLUSIVE: enemy/config missing"); yield break; }
        until = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < until && enemy != null && enemy.IsTeleporting()) yield return null;
        if (enemy == null || enemy.IsTeleporting()) { Debug.LogError("Cactus ground probe INCONCLUSIVE: teleport"); yield break; }
        var controller = typeof(Enemy).GetField("specialAttackController", Private).GetValue(enemy) as SpecialAttackController;
        if (controller == null) { Debug.LogError("Cactus ground probe INCONCLUSIVE: controller"); yield break; }
        manager.enabledWaves = false; enemy.DisableSpecialAttacks();
        typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, true);
        var body = player.playerMovement.GetRb(); body.velocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll; Physics.SyncTransforms();
        int savedMask = GameManager.Instance.whatIsGround.value;
        for (int mode = 0; mode < 2; mode++)
        {
            Vector3 target = player.transform.position;
            GameManager.Instance.whatIsGround = mode == 0 ? 0 : savedMask;
            var hits = Physics.RaycastAll(target + Vector3.up * 999f, Vector3.down, 9999f, GameManager.Instance.whatIsGround);
            Vector3 expected = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
            if (mode == 1 && hits.Length == 0) { GameManager.Instance.whatIsGround = savedMask; Debug.LogError("Cactus ground probe INCONCLUSIVE: no actual terrain"); yield break; }
            typeof(SpecialAttackController).GetMethod("UseSpecialAttack", Private).Invoke(controller, new object[] { config });
            if (!PoolManager.Instance.enemyAttacksPools.TryGetValue(config.attackName, out var pool) || pool.CountActive == 0) { GameManager.Instance.whatIsGround = savedMask; Debug.LogError("Cactus ground probe FAIL: no attack"); yield break; }
            var active = FindObjectsByType<EnemySpecialAttackCactusProjectile>(FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.activeInHierarchy);
            if (active == null) { GameManager.Instance.whatIsGround = savedMask; Debug.LogError("Cactus ground probe INCONCLUSIVE: projectile missing"); yield break; }
            Vector3 actual = (Vector3)typeof(EnemySpecialAttackCactusProjectile).GetField("impactPos", Private).GetValue(active);
            float error = Vector3.Distance(actual, expected);
            until = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < until && pool.CountActive > 0) yield return null;
            bool ok = error < .01f && pool.CountActive == 0 && (mode != 0 || actual == Vector3.zero);
            Debug.Log($"Cactus ground probe {(ok ? "PASS" : "FAIL")}: mode={(mode == 0 ? "no-ground" : "terrain")}, hits={hits.Length}, target={actual}, expected={expected}, error={error:F3}, poolActive={pool.CountActive}");
            if (!ok) { GameManager.Instance.whatIsGround = savedMask; yield break; }
        }
        GameManager.Instance.whatIsGround = savedMask;
    }
}
#endif
