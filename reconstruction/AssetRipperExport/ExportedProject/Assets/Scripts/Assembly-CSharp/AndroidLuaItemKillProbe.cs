#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaItemKillProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_kill_probe", false)) return;
        var go = new GameObject("Lua item kill probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaItemKillProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk-item-kill", "training-cell");
        if (player == null || player.inventory == null || manager == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaEnemyKilled) { Debug.LogError("Lua kill probe INCONCLUSIVE: run or item unavailable"); yield break; }
        manager.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; inv.playerStats.ForceUpdateStats(); float baseline = PlayerStats.GetStat(EStat.PowerupChance);
        inv.itemInventory.AddItem(id); inv.playerStats.ForceUpdateStats(); float initial = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        var enemy = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        end = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < end && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Lua kill probe INCONCLUSIVE: enemy unavailable"); yield break; }
        enemy.Kill("Lua kill test");
        end = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < end && !enemy.IsDeadOrDyingNextFrame()) yield return null;
        yield return new WaitForSecondsRealtime(1f);
        inv.playerStats.ForceUpdateStats(); float killed = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.itemInventory.RemoveItem(id); inv.playerStats.ForceUpdateStats(); float removed = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        bool pass = Math.Abs(initial - .05f) < .001f && Math.Abs(killed - .1f) < .001f && Math.Abs(removed) < .001f;
        Debug.Log($"Lua kill probe {(pass ? "PASS" : "FAIL")}: initial={initial:F3} afterKill={killed:F3} removed={removed:F3}");
    }
}
#endif
