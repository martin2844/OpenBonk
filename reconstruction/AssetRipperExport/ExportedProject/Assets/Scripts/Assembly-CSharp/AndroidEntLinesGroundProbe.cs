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

internal sealed class AndroidEntLinesGroundProbe : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_ent_lines_ground_probe", false)) return;
        var go = new GameObject("Ent lines ground probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidEntLinesGroundProbe>();
    }
    IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < until && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Ent lines ground probe INCONCLUSIVE: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetEnemyData(EEnemy.Ent1);
        var config = data == null ? null : data.specialAttacks.FirstOrDefault(a => a.attackName == "RootAttackLines3");
        var ent = data == null ? null : manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 8f, -1, true, canBeElite:false);
        if (ent == null || config == null) { Debug.LogError("Ent lines ground probe INCONCLUSIVE: Ent/config missing"); yield break; }
        until = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < until && ent != null && ent.IsTeleporting()) yield return null;
        if (ent == null || ent.IsTeleporting()) { Debug.LogError("Ent lines ground probe INCONCLUSIVE: teleport"); yield break; }
        var controller = typeof(Enemy).GetField("specialAttackController", Private).GetValue(ent) as SpecialAttackController;
        if (controller == null) { Debug.LogError("Ent lines ground probe INCONCLUSIVE: controller"); yield break; }
        typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, true);
        foreach (var other in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if (other != ent) other.gameObject.SetActive(false);
        var body = player.playerMovement.GetRb(); body.velocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll; Physics.SyncTransforms();
        typeof(SpecialAttackController).GetMethod("UseSpecialAttack", Private).Invoke(controller, new object[] { config });
        if (!PoolManager.Instance.enemyAttacksPools.TryGetValue(config.attackName, out var pool) || pool.CountActive == 0) { Debug.LogError("Ent lines ground probe FAIL: attack did not start"); yield break; }
        int checks = 0, mismatches = 0; float maxError = 0f;
        until = Time.realtimeSinceStartup + 80f;
        while (Time.realtimeSinceStartup < until && pool.CountActive > 0)
        {
            foreach (var warning in FindObjectsByType<CircleWarning>(FindObjectsSortMode.None))
            {
                Vector3 point = warning.transform.position;
                var hits = Physics.RaycastAll(point + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
                if (hits.Length == 0) continue;
                var ground = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
                float error = Mathf.Abs(point.y - ground.y); checks++; maxError = Mathf.Max(maxError, error); if (error > .1f) mismatches++;
            }
            yield return null;
        }
        bool success = checks > 0 && mismatches == 0 && pool.CountActive == 0;
        Debug.Log($"Ent lines ground probe {(success ? "PASS" : "FAIL")}: checks={checks}, mismatches={mismatches}, maxHeightError={maxError:F3}, poolActive={pool.CountActive}, groundMask={GameManager.Instance.whatIsGround.value}");
    }
}
#endif
