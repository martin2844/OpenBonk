#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable diagnostic: boss roster, spawning and damage are controlled.
internal sealed class AndroidMultiBossHudProbe : MonoBehaviour
{
    bool checkRelease;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_multiboss_hud_probe", false)) return;
        var root = new GameObject("multi boss HUD probe"); DontDestroyOnLoad(root); var probe = root.AddComponent<AndroidMultiBossHudProbe>();
        probe.checkRelease = intent.Call<bool>("getBooleanExtra", "megabonk_multiboss_release_probe", false);
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || FindFirstObjectByType<TargetOfInterestUi>() == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance; var ui = FindFirstObjectByType<TargetOfInterestUi>();
        if (player == null || player.inventory == null || manager == null || ui == null) { Debug.LogError("Multi boss HUD probe FAIL: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        // MyPlayer.FixedUpdate clears the flag at the cooldown deadline, so
        // holding only the flag for one frame does not protect this probe.
        var invincibleField = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var cooldownField = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincibleField.GetValue(player);
        float oldCooldown = (float)cooldownField.GetValue(player);
        cooldownField.SetValue(player, float.PositiveInfinity); invincibleField.SetValue(player, true);
        var types = new[] { EEnemy.Ent1, EEnemy.Pharaoh1, EEnemy.GhostKing, EEnemy.Ent1, EEnemy.Pharaoh1, EEnemy.GhostKing, EEnemy.Ent1 };
        var spawned = new List<Enemy>();
        for (int i = 0; i < types.Length; i++)
        {
            var position = player.transform.position + player.transform.forward * 30f + player.transform.right * ((i - 3) * 7f);
            var boss = manager.SpawnBoss(types[i], -1, EEnemyFlag.Boss, position);
            if (boss == null) { Debug.LogError("Multi boss HUD probe FAIL: spawn " + types[i]); yield break; }
            boss.DisableSpecialAttacks(); spawned.Add(boss);
        }
        var enemyField = typeof(TargetOfInterestPrefab).GetField("enemy", BindingFlags.NonPublic | BindingFlags.Instance);
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && spawned.Any(b => !b.CanTakeDamage() || !ui.prefabs.Any(r => r != null && (Enemy)enemyField.GetValue(r) == b && r.gameObject.activeInHierarchy && r.canvasGroup != null && r.canvasGroup.alpha > .95f))) yield return null;
        int passed = 0;
        foreach (var boss in spawned)
        {
            var row = ui.prefabs.FirstOrDefault(r => r != null && (Enemy)enemyField.GetValue(r) == boss);
            if (row == null || row.hpBar == null || row.t_hp == null || row.t_name == null || !boss.CanTakeDamage()) { Debug.LogError("Multi boss HUD probe FAIL: row or damageable state " + boss.enemyData.enemyName); continue; }
            float before = boss.hp; float scale = row.hpBar.transform.localScale.x; string label = row.t_hp.text;
            boss.DamageExternal(new DamageContainer(0f, "MultiBossHudProbe") { damage = boss.maxHp * .25f, knockback = 0f, direction = Vector3.zero });
            yield return null;
            bool ok = boss.hp < before && row.hpBar.transform.localScale.x < scale && row.t_hp.text != label && !string.IsNullOrWhiteSpace(row.t_name.text);
            if (ok) passed++;
            Debug.Log($"Multi boss HUD probe {(ok ? "PASS" : "FAIL")}: type={boss.enemyData.enemyName}, name={row.t_name.text}, hp={before:F2}->{boss.hp:F2}, scale={scale:F3}->{row.hpBar.transform.localScale.x:F3}, label={row.t_hp.text}");
        }
        if (passed == types.Length) Debug.Log("Multi boss HUD probe RESULT PASS: seven concurrent bosses");
        else Debug.LogError($"Multi boss HUD probe RESULT FAIL: {passed}/{types.Length}");
        Debug.Log("Multi boss HUD probe: holding scene for visual capture");
        yield return new WaitForSecondsRealtime(checkRelease ? 3f : 20f);
        if (checkRelease)
        {
            Enemy removed = spawned[1];
            foreach (var boss in spawned) if (boss != null && boss.enemyMovement != null) boss.enemyMovement.StopMovement();
            deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline && !removed.CanTakeDamage()) yield return null;
            bool killReady = removed.CanTakeDamage(); float hpBeforeKill = removed.hp;
            if (killReady) removed.DamageExternal(new DamageContainer(0f, "MultiBossHudProbe") { damage = removed.maxHp * 10f, flags = DcFlags.BypassAll, knockback = 0f, direction = Vector3.zero });
            Debug.Log($"Multi boss HUD release setup: damageable={killReady}, state={removed.state}, hp={hpBeforeKill:F2}->{removed.hp:F2}, active={removed.gameObject.activeInHierarchy}");
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && (removed.gameObject.activeInHierarchy || ui.prefabs.Any(r => r != null && (Enemy)enemyField.GetValue(r) == removed))) yield return null;
            bool gone = !removed.gameObject.activeInHierarchy && !ui.prefabs.Any(r => r != null && (Enemy)enemyField.GetValue(r) == removed);
            bool survivors = spawned.Where((_, i) => i != 1).All(b => b != null && b.gameObject.activeInHierarchy && ui.prefabs.Any(r => r != null && r.gameObject.activeInHierarchy && (Enemy)enemyField.GetValue(r) == b));
            Debug.Log($"Multi boss HUD release probe {(!killReady ? "INCONCLUSIVE" : gone && survivors ? "PASS" : "FAIL")}: removed={gone}, otherRows={survivors}, hp={removed.hp:F2}, state={removed.state}, active={removed.gameObject.activeInHierarchy}");
            yield return new WaitForSecondsRealtime(5f);
        }
        if (player != null) { cooldownField.SetValue(player, oldCooldown); invincibleField.SetValue(player, oldInvincible); }
    }
}
#endif
