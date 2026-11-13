#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaGoldRewardProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_gold_reward_probe", false)) return;
        var go = new GameObject("Lua gold reward probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaGoldRewardProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk.example.goldreward", "gold-reward");
        if (player == null || player.inventory == null || manager == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaEnemyKilled) { Debug.LogError("Lua gold reward INCONCLUSIVE: item unavailable"); yield break; }
        manager.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; int before = inv.goldInt;
        inv.itemInventory.AddItem(id);
        var enemy = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        end = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < end && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { inv.itemInventory.RemoveItem(id); Debug.LogError("Lua gold reward INCONCLUSIVE: enemy unavailable"); yield break; }
        enemy.Kill("Lua gold reward test");
        yield return new WaitForSecondsRealtime(1f);
        int after = inv.goldInt;
        inv.itemInventory.RemoveItem(id);
        Debug.Log($"Lua gold reward {(after > before ? "PASS" : "FAIL")}: gold={before}->{after}");
    }
}
#endif
