#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaIncomingDamageProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_incoming_damage_probe", false)) return;
        var go = new GameObject("Lua incoming damage probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaIncomingDamageProbe>();
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-incoming-test", "training-cell");
        if (player == null || player.inventory == null || EnemyManager.Instance == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaBeforePlayerDamage) { Debug.LogError("Lua incoming INCONCLUSIVE: run or item unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false; foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; var health = inv.playerHealth;
        bool ready1 = health.CanTakeDamage();
        int first = health.hp; health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua incoming test", DcFlags.BypassAll); int baseline = first - health.hp;
        yield return new WaitForSecondsRealtime(2f);
        inv.itemInventory.AddItem(id); bool ready2 = health.CanTakeDamage(); int second = health.hp; health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua incoming test", DcFlags.BypassAll); int modified = second - health.hp;
        inv.itemInventory.RemoveItem(id); yield return new WaitForSecondsRealtime(2f);
        bool ready3 = health.CanTakeDamage(); int third = health.hp; health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua incoming test", DcFlags.BypassAll); int restored = third - health.hp;
        data.itemTickPriority = 0;
        var secondMod = Register("ordered", data, "return {before_player_damage=function(d) if d == 5 then return 2 else return 0 end end}", 1);
        var failureMod = Register("failure", data, "return {before_player_damage=function(d) error('expected failure') end}", 2);
        var zeroMod = Register("zero", data, "return {before_player_damage=function(d) return 0 end}", 2);
        yield return new WaitForSecondsRealtime(2f);
        inv.itemInventory.AddItem(id); inv.itemInventory.AddItem(secondMod);
        health.shield = 40f; int hpBeforeShield = health.hp;
        health.DamagePlayerExternal(10f, 0f, Vector3.up, false, "Lua incoming test", DcFlags.BypassAll);
        float shieldLoss = 40f - health.shield; bool hpUnchanged = health.hp == hpBeforeShield;
        inv.itemInventory.RemoveItem(secondMod); inv.itemInventory.RemoveItem(id);
        yield return new WaitForSecondsRealtime(2f);
        inv.itemInventory.AddItem(failureMod); int hpBeforeFailure = health.hp;
        health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua incoming test", DcFlags.BypassAll);
        int failedCallbackLoss = hpBeforeFailure - health.hp; inv.itemInventory.RemoveItem(failureMod);
        yield return new WaitForSecondsRealtime(2f);
        inv.itemInventory.AddItem(zeroMod); int hpBeforeZero = health.hp;
        health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "Lua incoming test", DcFlags.BypassAll);
        int zeroLoss = hpBeforeZero - health.hp; inv.itemInventory.RemoveItem(zeroMod);
        bool pass = baseline == 10 && modified == 5 && restored == 10 && ready1 && ready2 && ready3 && Math.Abs(shieldLoss - 10f) < .001f && hpUnchanged && failedCallbackLoss == 10 && zeroLoss == 0;
        Debug.Log($"Lua incoming {(pass ? "PASS" : "FAIL")}: baseline={baseline} modified={modified} removed={restored} ready={ready1}/{ready2}/{ready3} shieldLoss={shieldLoss:F1} shieldHpUnchanged={hpUnchanged} failedCallbackLoss={failedCallbackLoss} zeroLoss={zeroLoss}");
    }
    private static EItem Register(string key, OpenBonkItemData template, string source, int priority)
    {
        var item = ScriptableObject.CreateInstance<OpenBonkItemData>();
        item.isEnabled = true; item.icon = template.icon; item.rarity = template.rarity; item.bonuses = Array.Empty<OpenBonkItemBonus>();
        item.externalId = "openbonk-probe:" + key; item.itemTickPriority = priority; item.luaBeforePlayerDamage = true; item.luaSource = source;
        DataManager.Instance.RegisterExternalItem("openbonk-probe", key, item);
        return (EItem)OpenBonkContentIds.Item("openbonk-probe", key);
    }
}
#endif
