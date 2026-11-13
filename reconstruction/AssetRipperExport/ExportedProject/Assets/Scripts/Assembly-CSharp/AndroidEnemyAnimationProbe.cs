#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Controlled roster of enemy prefabs in Forest; their normal mesh Update remains active.
internal sealed class AndroidEnemyAnimationProbe : MonoBehaviour
{
    static bool installed;
    static bool beeOnly;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_enemy_animation_probe", false)) return;
        beeOnly = intent.Call<bool>("getBooleanExtra", "megabonk_enemy_animation_bee", false);
        installed = true;
        var root = new GameObject("enemy animation probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidEnemyAnimationProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Enemy animation probe INCONCLUSIVE: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, float.MaxValue);
        int startHp = player.inventory.playerHealth.hp;
        var types = beeOnly ? new[] { EEnemy.Bee } : new[] { EEnemy.Goblin, EEnemy.Ent1, EEnemy.MinibossGolem, EEnemy.Scorpion, EEnemy.Pharaoh1, EEnemy.GhostKing, EEnemy.Zombie, EEnemy.Skeleton };
        var enemies = new List<Enemy>(); var meshes = new List<HashSet<int>>();
        for (int i = 0; i < types.Length; i++)
        {
            var data = DataManager.Instance.GetEnemyData(types[i]);
            var pos = player.transform.position + player.transform.forward * (8f + 3f * (i / 4)) + player.transform.right * ((i % 4 - 1.5f) * 3f);
            var enemy = data != null ? manager.SpawnEnemy(data, pos, -1, true, canBeElite: false) : null;
            if (enemy == null || enemy.animatedMesh == null) { Debug.LogError("Enemy animation probe FAIL spawn=" + types[i]); continue; }
            enemy.DisableSpecialAttacks(); enemies.Add(enemy); meshes.Add(new HashSet<int>());
        }
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var filter = enemy != null && enemy.animatedMesh != null ? enemy.animatedMesh.GetComponent<MeshFilter>() : null;
                if (filter != null && filter.sharedMesh != null) meshes[i].Add(filter.sharedMesh.GetInstanceID());
            }
            yield return null;
        }
        int passed = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i]; bool ok = enemy != null && meshes[i].Count > 1;
            if (ok) passed++;
            Debug.Log($"Enemy animation probe {(ok ? "PASS" : "INCONCLUSIVE")}: type={(enemy != null && enemy.enemyData != null ? enemy.enemyData.enemyName.ToString() : "missing")}, observedMeshes={meshes[i].Count}");
        }
        Debug.Log($"Enemy animation probe RESULT passed={passed} spawned={enemies.Count} requested={types.Length}");
        Debug.Log($"Enemy animation probe protection hp={startHp}->{player.inventory.playerHealth.hp} canTakeDamage={player.CanTakeDamage()}");
    }
}
#endif
