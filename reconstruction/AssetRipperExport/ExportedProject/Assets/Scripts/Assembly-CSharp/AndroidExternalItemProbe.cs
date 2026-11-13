#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;

// Explicit-intent emulator probe for an imported, enabled item package.
internal sealed class AndroidExternalItemProbe : MonoBehaviour
{
    private PlayerInventory inventory;
    private bool lua;
    private static bool luaProbe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_probe", false)) return;
        luaProbe = intent.Call<bool>("getBooleanExtra", "openbonk_item_lua_probe", false);
        MyPlayer.A_PlayerInventoryInitialized -= Observe; MyPlayer.A_PlayerInventoryInitialized += Observe;
    }
    private static void Observe(PlayerInventory inventory)
    {
        MyPlayer.A_PlayerInventoryInitialized -= Observe;
        var go = new GameObject("External item probe"); DontDestroyOnLoad(go);
        var probe = go.AddComponent<AndroidExternalItemProbe>(); probe.inventory = inventory; probe.lua = luaProbe;
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 40f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || RunUnlockables.availableItems == null)) yield return null;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || RunUnlockables.availableItems == null) { Debug.LogWarning("External item probe INCONCLUSIVE: no active run"); Destroy(gameObject); yield break; }
        var id = (EItem)OpenBonkContentIds.Item(lua ? "openbonk-item-lua-fixture" : "openbonk-item-fixture", "training-cell");
        if (!DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData) || !RunUnlockables.availableItems[item.rarity].Contains(item))
        { Debug.LogError("External item probe FAIL: item not registered in drop pool"); Destroy(gameObject); yield break; }
        inventory.playerStats.ForceUpdateStats();
        float baseChance = PlayerStats.GetStat(EStat.PowerupChance), baseDuration = PlayerStats.GetStat(EStat.DurationMultiplier);
        inventory.itemInventory.AddItem(id); inventory.playerStats.ForceUpdateStats();
        float chance1 = PlayerStats.GetStat(EStat.PowerupChance), duration1 = PlayerStats.GetStat(EStat.DurationMultiplier);
        float attack1 = lua ? AttackMultiplier() : 1f;
        inventory.itemInventory.AddItem(id); inventory.playerStats.ForceUpdateStats();
        float chance2 = PlayerStats.GetStat(EStat.PowerupChance), duration2 = PlayerStats.GetStat(EStat.DurationMultiplier);
        float attack2 = lua ? AttackMultiplier() : 1f;
        inventory.itemInventory.RemoveItem(id); inventory.itemInventory.RemoveItem(id); inventory.playerStats.ForceUpdateStats();
        float afterChance = PlayerStats.GetStat(EStat.PowerupChance), afterDuration = PlayerStats.GetStat(EStat.DurationMultiplier);
        float repeat2 = 0f;
        if (lua)
        {
            inventory.itemInventory.AddItem(id); inventory.itemInventory.AddItem(id); inventory.playerStats.ForceUpdateStats();
            repeat2 = PlayerStats.GetStat(EStat.PowerupChance);
            inventory.itemInventory.RemoveItem(id); inventory.itemInventory.RemoveItem(id); inventory.playerStats.ForceUpdateStats();
        }
        bool ok = Near(chance1-baseChance, .05f) && Near(duration1-baseDuration, .02f) && Near(chance2-baseChance, lua ? .15f : .10f) && Near(duration2-baseDuration, lua ? .06f : .04f) && Near(afterChance, baseChance) && Near(afterDuration, baseDuration) && inventory.itemInventory.GetAmount(id) == 0 && (!lua || (Near(attack1, 1.25f) && Near(attack2, 1.5f) && Near(repeat2-baseChance, .15f)));
        Debug.Log($"External item probe {(ok ? "PASS" : "FAIL")}: lua={lua} pool=true base={baseChance:F3}/{baseDuration:F3} stack1={chance1:F3}/{duration1:F3} stack2={chance2:F3}/{duration2:F3} removed={afterChance:F3}/{afterDuration:F3} attacks={attack1:F2}/{attack2:F2} repeated={repeat2:F3}");
        Destroy(gameObject);
    }
    private static bool Near(float a, float b) => Math.Abs(a-b) < .0001f;
    private float AttackMultiplier() => inventory.itemInventory.PreAttack(new DamageContainer(1f, "item-probe"), new StatComponents()).multiplicativeValue;
}
#endif
