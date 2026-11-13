#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in event invocation in an isolated test run; terrain sampling and enemy creation remain real.
internal sealed class AndroidCursedMinibossProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_cursed_miniboss_probe", false)) return;
        var go = new GameObject("Cursed miniboss probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidCursedMinibossProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || EnemyManager.Instance.summonerController == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance; var game = GameManager.Instance;
        if (player == null || manager == null || game == null || manager.summonerController == null) { Debug.LogError("Cursed miniboss probe: scene unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var cooldown = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); float oldCooldown = (float)cooldown.GetValue(player);
        cooldown.SetValue(player, float.PositiveInfinity); invincible.SetValue(player, true);
        int oldCurses = game.bossCurses; game.bossCurses = 7;
        int before = manager.summonerController.minibossCount, notifications = 0;
        var spawned = new List<Enemy>();
        Enemy.A_EnemySpawned += OnSpawn; SummonerController.A_MiniBoss += OnMiniboss;
        manager.summonerController.EventMiniboss(null);
        Enemy.A_EnemySpawned -= OnSpawn; SummonerController.A_MiniBoss -= OnMiniboss;
        Debug.Log($"Cursed miniboss probe: curses=7, spawned={spawned.Count}, notifications={notifications}, eventCountBefore={before}, eventCountAfter={manager.summonerController.minibossCount}");
        yield return new WaitForSecondsRealtime(4f);
        int alive = 0; foreach (var enemy in spawned) if (enemy != null && enemy.gameObject.activeInHierarchy) alive++;
        var ui = FindFirstObjectByType<TargetOfInterestUi>();
        var target = typeof(TargetOfInterestPrefab).GetField("enemy", BindingFlags.NonPublic | BindingFlags.Instance);
        int displayed = ui == null ? 0 : spawned.Count(e => e != null && ui.prefabs.Any(row => row != null && row.gameObject.activeInHierarchy && (Enemy)target.GetValue(row) == e));
        bool pass = spawned.Count == 8 && alive == 8 && displayed == 8 && notifications == 1 && manager.summonerController.minibossCount == before + 1;
        Debug.Log($"Cursed miniboss probe {(pass ? "PASS" : "INCONCLUSIVE")}: alive={alive}, spawned={spawned.Count}, displayed={displayed}, notifications={notifications}");
        yield return new WaitForSecondsRealtime(12f);
        game.bossCurses = oldCurses; if (player != null) { invincible.SetValue(player, oldInvincible); cooldown.SetValue(player, oldCooldown); } if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
        void OnSpawn(Enemy e) { spawned.Add(e); Debug.Log($"Cursed miniboss probe: enemy={e.enemyData.enemyName}, boss={e.IsBoss()}, position={e.transform.position}"); }
        void OnMiniboss() { notifications++; }
    }
}
#endif
