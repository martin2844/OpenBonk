#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaEnemyContextProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_lua_enemy_context_probe", false)) return;
        var go = new GameObject("Lua enemy context probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaEnemyContextProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-item-damage", "training-cell");
        if (player == null || player.inventory == null || EnemyManager.Instance == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaPlayerDamaged || !data.luaBeforePlayerDamage) { Debug.LogError("Lua enemy context INCONCLUSIVE: item or run unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        foreach (var old in new List<Enemy>(EnemyManager.Instance.enemies.Values)) if (old != null && old.gameObject.activeInHierarchy) old.ReleaseToPool();
        var inv = player.inventory; var health = inv.playerHealth;
        inv.playerStats.ForceUpdateStats(); float baseline = PlayerStats.GetStat(EStat.PowerupChance);
        inv.itemInventory.AddItem(id); health.hp = health.maxHp = 100; health.shield = 0;
        var enemyData = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        var enemy = enemyData == null ? null : EnemyManager.Instance.SpawnEnemy(enemyData, player.transform.position + Vector3.right * 5f, -904, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Lua enemy context INCONCLUSIVE: no goblin"); yield break; }
        float observed = -1; int events = 0, collisions = 0;
        void Damage(PlayerHealth h, DamageContainer dc, bool shield) { if (h == health && dc.enemy == enemy && !shield) { observed = dc.damage; events++; } }
        void Collision() { collisions++; }
        PlayerHealth.A_TakeDamage += Damage; MyPlayer.A_CollidedEnemy += Collision;
        try {
            end = Time.realtimeSinceStartup + 35f;
            while (Time.realtimeSinceStartup < end && enemy != null && enemy.gameObject.activeInHierarchy && events == 0) yield return null;
        } finally { PlayerHealth.A_TakeDamage -= Damage; MyPlayer.A_CollidedEnemy -= Collision; }
        inv.playerStats.ForceUpdateStats(); float bonus = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        bool pass = events == 1 && collisions == 1 && observed > 0f && health.hp == (int)Mathf.Max(0, 100f - observed) && Math.Abs(bonus - .1f) < .001f;
        Debug.Log($"Lua enemy context {(pass ? "PASS" : "FAIL")}: collisions={collisions} events={events} damage={observed:F3} hp={health.hp} bonus={bonus:F3}");
        inv.itemInventory.RemoveItem(id); if (enemy != null) enemy.ReleaseToPool();
    }
}
#endif
