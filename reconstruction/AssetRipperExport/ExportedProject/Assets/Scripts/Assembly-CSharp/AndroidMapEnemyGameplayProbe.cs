#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in disposable-run probe of actual enemy velocity and player-health damage.
internal sealed class AndroidMapEnemyGameplayProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_map_enemy_gameplay_probe", false)) return;
        var root = new GameObject("Map enemy gameplay probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMapEnemyGameplayProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MapGenerationController.isGenerating || EnemyManager.Instance == null || DataManager.Instance == null || !(MapController.currentMap is OpenBonkExternalMapData))) yield return null;
        var player = MyPlayer.Instance; var map = MapController.currentMap as OpenBonkExternalMapData;
        if (player == null || map == null || !map.luaStageEnemySpeedMultiplier || !map.luaStageEnemyDamageMultiplier || EnemyManager.Instance == null) { Debug.LogError("Map enemy gameplay probe INCONCLUSIVE: setup unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        foreach (var old in new List<Enemy>(EnemyManager.Instance.enemies.Values)) if (old != null && old.gameObject.activeInHierarchy) old.ReleaseToPool();
        var data = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        var enemy = data == null ? null : EnemyManager.Instance.SpawnEnemy(data, player.transform.position + Vector3.right * 5f, -903, true, canBeElite: false);
        if (enemy == null || enemy.enemyMovement == null || enemy.enemyMovement.rb == null) { Debug.LogError("Map enemy gameplay probe INCONCLUSIVE: enemy unavailable"); yield break; }
        Vector3 start = enemy.enemyMovement.rb.position; float expectedSpeed = enemy.GetSpeed(), sampledSpeed = 0f;
        var health = player.inventory.playerHealth;
        float expectedDamage = DamageUtility.GetPlayerDamage(enemy, Vector3.zero, DcFlags.None).damage;
        float observedDamage = -1f; int events = 0, collisions = 0; int before = health.hp;
        void OnDamage(PlayerHealth damaged, DamageContainer dc, bool shield) { if (damaged == health && dc.enemy == enemy && !shield) { observedDamage = dc.damage; events++; } }
        void OnCollision() { collisions++; }
        PlayerHealth.A_TakeDamage += OnDamage; MyPlayer.A_CollidedEnemy += OnCollision;
        try
        {
            float motionDeadline = Time.realtimeSinceStartup + 35f;
            while (Time.realtimeSinceStartup < motionDeadline && enemy != null && enemy.gameObject.activeInHierarchy && events == 0)
            {
                Vector3 v = enemy.enemyMovement.rb.velocity; float speed = new Vector2(v.x, v.z).magnitude;
                if (speed > .1f && Vector3.Distance(start, enemy.enemyMovement.rb.position) > .05f) sampledSpeed = speed;
                yield return null;
            }
        }
        finally { PlayerHealth.A_TakeDamage -= OnDamage; MyPlayer.A_CollidedEnemy -= OnCollision; }
        int after = health.hp;
        bool motion = expectedSpeed > 0f && sampledSpeed > 0f && Mathf.Abs(sampledSpeed / expectedSpeed - 1f) < .15f;
        bool damage = events == 1 && collisions == 1 && expectedDamage > 0f && Mathf.Abs(observedDamage / expectedDamage - 1f) < .03f && after == (int)Mathf.Max(0f, before - observedDamage);
        Debug.Log($"Map enemy collision probe {(motion && damage ? "PASS" : "FAIL")}: expectedSpeed={expectedSpeed:F3} actualSpeed={sampledSpeed:F3} distance={Vector3.Distance(start, enemy.enemyMovement.rb.position):F3} expectedDamage={expectedDamage:F3} observedDamage={observedDamage:F3} collisions={collisions} events={events} hp={before}->{after}");
        enemy.ReleaseToPool();
    }
}
#endif
