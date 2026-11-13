#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaItemLevelProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_level_probe", false)) return;
        var obj = new GameObject("Lua item level probe"); DontDestroyOnLoad(obj); obj.AddComponent<AndroidLuaItemLevelProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var p = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-item-levelup", "training-cell");
        if (p == null || p.inventory == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaLevelUp || p.inventory.playerXp.level != 0) { Debug.LogError("Lua item level probe INCONCLUSIVE: run or item unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var w in p.inventory.weaponInventory.weapons.Values) w.Disable();
        var inv = p.inventory; inv.playerStats.ForceUpdateStats(); float baseline = PlayerStats.GetStat(EStat.PowerupChance);
        inv.itemInventory.AddItem(id); inv.playerStats.ForceUpdateStats(); float once = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.playerXp.AddXp(XpUtility.LevelToXp(3)); inv.playerStats.ForceUpdateStats(); float leveled = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        int level = inv.playerXp.level;
        inv.itemInventory.AddItem(id); inv.playerStats.ForceUpdateStats(); float twice = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.itemInventory.RemoveItem(id); inv.itemInventory.RemoveItem(id); inv.playerStats.ForceUpdateStats(); float removed = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.playerXp.AddXp(XpUtility.LevelToXp(4)); inv.playerStats.ForceUpdateStats(); float after = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        bool pass = level == 3 && Near(once,.05f) && Near(leveled,.15f) && Near(twice,.6f) && Near(removed,0f) && Near(after,0f);
        Debug.Log($"Lua item level probe {(pass ? "PASS" : "FAIL")}: level={level} initial={once:F3} afterLevel={leveled:F3} twoStacks={twice:F3} removed={removed:F3} afterRemovalLevel={after:F3}");
    }
    private static bool Near(float a, float b) => Math.Abs(a-b) < .001f;
}
#endif
