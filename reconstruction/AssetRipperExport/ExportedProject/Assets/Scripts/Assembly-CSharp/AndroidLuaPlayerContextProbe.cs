#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaPlayerContextProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_player_context_probe", false)) return;
        var go = new GameObject("Lua player context probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaPlayerContextProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk-item-damage", "training-cell");
        if (player == null || player.inventory == null || EnemyManager.Instance == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaPlayerDamaged || !data.luaBeforePlayerDamage) { Debug.LogError("Lua player context INCONCLUSIVE: item or run unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; var health = inv.playerHealth;
        inv.playerStats.ForceUpdateStats(); float baseline = PlayerStats.GetStat(EStat.PowerupChance);
        inv.itemInventory.AddItem(id); health.hp = health.maxHp = 100; health.shield = health.maxShield = 40;
        health.DamagePlayerExternal(10f, 0f, Vector3.up, false, "Lua context", DcFlags.BypassAll);
        inv.playerStats.ForceUpdateStats(); float shieldScale = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        bool shieldOk = health.shield == 35f && health.hp == 100 && Math.Abs(shieldScale - .15f) < .001f;
        yield return new WaitForSecondsRealtime(2f);
        health.shield = 0;
        health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua context", DcFlags.BypassAll);
        inv.playerStats.ForceUpdateStats(); float hpScale = PlayerStats.GetStat(EStat.PowerupChance) - baseline;
        bool hpOk = health.hp == 95 && Math.Abs(hpScale - .10f) < .001f;
        inv.itemInventory.RemoveItem(id); inv.playerStats.ForceUpdateStats();
        bool removed = Math.Abs(PlayerStats.GetStat(EStat.PowerupChance) - baseline) < .001f;
        Debug.Log($"Lua player context {(shieldOk && hpOk && removed ? "PASS" : "FAIL")}: shield={health.shield} shieldScale={shieldScale:F3} hp={health.hp} hpScale={hpScale:F3} removed={removed}");
    }
}
#endif
