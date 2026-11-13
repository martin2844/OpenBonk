#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaItemDamageProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_damage_probe", false)) return;
        var go = new GameObject("Lua item damage probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaItemDamageProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-item-damage", "training-cell");
        if (player == null || player.inventory == null || EnemyManager.Instance == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaPlayerDamaged) { Debug.LogError("Lua damage probe INCONCLUSIVE: run or item unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; inv.playerStats.ForceUpdateStats(); float baseline = PlayerStats.GetStat(EStat.PowerupChance);
        inv.itemInventory.AddItem(id); inv.playerStats.ForceUpdateStats(); float initial = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua test");
        inv.playerStats.ForceUpdateStats(); float hit = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.itemInventory.RemoveItem(id); inv.playerStats.ForceUpdateStats(); float removed = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        yield return new WaitForSecondsRealtime(2f);
        inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua test");
        inv.playerStats.ForceUpdateStats(); float after = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        inv.itemInventory.AddItem(id); inv.playerStats.ForceUpdateStats(); float reacquired = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        bool pass = Near(initial,.05f) && Near(hit,.1f) && Near(removed,0f) && Near(after,0f) && Near(reacquired,.05f);
        Debug.Log($"Lua damage probe {(pass ? "PASS" : "FAIL")}: initial={initial:F3} hit={hit:F3} removed={removed:F3} afterRemoval={after:F3} reacquired={reacquired:F3}");
    }
    private static bool Near(float a, float b) => Math.Abs(a-b) < .001f;
}
#endif
