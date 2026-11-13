#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

internal sealed class AndroidLuaKillRewardProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_kill_reward_probe", false)) return;
        var go = new GameObject("Lua kill reward probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaKillRewardProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk.example.killreward", "kill-reward");
        if (player == null || player.inventory == null || manager == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaEnemyKilled || data.bonuses.Length != 0) { Debug.LogError("Lua kill reward INCONCLUSIVE: item unavailable"); yield break; }
        manager.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; var hp = inv.playerHealth; var xp = inv.playerXp;
        if (hp == null || xp == null || hp.maxHp < 20) { Debug.LogError("Lua kill reward INCONCLUSIVE: player unavailable"); yield break; }
        int oldHp = hp.hp; hp.hp = 10; int startXp = xp.xp;
        inv.itemInventory.AddItem(id);
        var enemy = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        end = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < end && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { inv.itemInventory.RemoveItem(id); hp.hp = oldHp; Debug.LogError("Lua kill reward INCONCLUSIVE: enemy unavailable"); yield break; }
        enemy.Kill("Lua kill reward test");
        yield return new WaitForSecondsRealtime(1f);
        inv.LateUpdate(); int afterHp = hp.hp, afterXp = xp.xp;
        inv.itemInventory.RemoveItem(id); hp.hp = oldHp;
        bool pass = afterHp == 11 && afterXp > startXp;
        Debug.Log($"Lua kill reward {(pass ? "PASS" : "FAIL")}: hp=10->{afterHp} xp={startXp}->{afterXp}");
    }
}
#endif
