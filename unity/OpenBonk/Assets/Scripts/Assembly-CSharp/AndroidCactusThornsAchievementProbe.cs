#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

// Isolated controlled hit: real damage, retaliation, enemy death and achievement flow.
internal sealed class AndroidCactusThornsAchievementProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_cactus_thorns_achievement_probe", false)) return;
        var go = new GameObject("Cactus thorns achievement probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidCactusThornsAchievementProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Cactus thorns probe: scene unavailable"); yield break; }
        var data = DataManager.Instance.GetEnemyData(EEnemy.CactusShooter);
        if (data == null) { Debug.LogError("Cactus thorns probe: cactus data unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        player.inventory.statInventory.ChangeStat(new StatModifier { stat = EStat.Thorns, modifyType = EStatModifyType.Flat, modification = 1000f }, false, 30f, false);
        player.inventory.statInventory.Tick();
        MyStats.SetValueForce("cactusKillsWithThorns", 99f);
        var enemy = manager.SpawnEnemy(data, player.transform.position + player.transform.forward * 4f, -1, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Cactus thorns probe: spawn failed"); Restore(); yield break; }
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && (!enemy.CanTakeDamage() || !player.inventory.playerHealth.CanTakeDamage())) yield return null;
        if (enemy == null || !enemy.CanTakeDamage() || !player.inventory.playerHealth.CanTakeDamage()) { Debug.LogError("Cactus thorns probe: enemy or player not damageable"); Restore(); yield break; }
        enemy.SetDiagnosticHealth(1f);
        Debug.Log($"Cactus thorns probe: before enemy={data.enemyName}, thorns={player.inventory.playerStats.GetStat(EStat.Thorns)}, count={MyStats.GetStat("cactusKillsWithThorns")}, unlocked={SaveManager.Instance.progression.achievements.Contains("a_cactus")}");
        player.inventory.playerHealth.DamagePlayerExternal(1f, 0f, Vector3.forward, false, "cactus_thorns_probe", enemy: enemy);
        yield return new WaitForSecondsRealtime(3f);
        Debug.Log($"Cactus thorns probe: after count={MyStats.GetStat("cactusKillsWithThorns")}, unlocked={SaveManager.Instance.progression.achievements.Contains("a_cactus")}");
        Restore();
        void Restore() { if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
