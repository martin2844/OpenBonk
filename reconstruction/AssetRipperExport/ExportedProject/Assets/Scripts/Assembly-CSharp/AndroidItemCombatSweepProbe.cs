#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in smoke test: one hit-processing item at a time with ordinary Firestaff attacks.
internal sealed class AndroidItemCombatSweepProbe : MonoBehaviour
{
    int weaponHits, otherHits, itemHits, spawns;
    string current;
    static string itemFilter;
    static int observationSeconds = 6;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_item_combat_sweep", false)) return;
        itemFilter = intent.Call<string>("getStringExtra", "megabonk_item_combat_filter");
        observationSeconds = Mathf.Clamp(intent.Call<int>("getIntExtra", "megabonk_item_combat_seconds", 6), 1, 60);
        var root = new GameObject("item combat sweep probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidItemCombatSweepProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Item combat sweep FAIL: game not ready"); yield break; }
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        var inventory = player.inventory.itemInventory;
        int tested=0, failed=0, withoutWeaponHits=0;
        foreach (var data in DataManager.Instance.unsortedItems)
        {
            if (data == null || !data.isEnabled) continue;
            var id = data.eItem;
            if (!string.IsNullOrEmpty(itemFilter) && !string.Equals(id.ToString(), itemFilter, StringComparison.Ordinal)) continue;
            // Classify without acquiring unrelated items: some have immediate
            // world effects which can open encounters and pause the combat.
            var candidate = ItemFactory.CreateItem(id, inventory);
            if (candidate == null || (!candidate.HasOnHitEffectProc() && !candidate.HasPreAttackProc())) continue;
            try { inventory.AddItem(id); }
            catch (Exception e) { failed++; Debug.LogError($"Item combat sweep exception: item={id} step=add {e}"); continue; }
            var instance = inventory.GetItem(id);
            if (instance == null) { failed++; Debug.LogError("Item combat sweep FAIL: missing item " + id); continue; }
            current=id.ToString(); weaponHits=0; otherHits=0; itemHits=0;
            Debug.Log($"Item combat sweep START: item={current} onHit={instance.HasOnHitEffectProc()} preAttack={instance.HasPreAttackProc()}");
            yield return new WaitForSecondsRealtime(observationSeconds);
            float hitDeadline = Time.realtimeSinceStartup + 14f;
            while (weaponHits == 0 && Time.realtimeSinceStartup < hitDeadline) yield return null;
            int recordedWeaponHits=weaponHits, recordedOtherHits=otherHits, recordedItemHits=itemHits;
            try { inventory.RemoveItem(id, false); }
            catch (Exception e) { failed++; Debug.LogError($"Item combat sweep exception: item={id} step=remove {e}"); }
            if (recordedWeaponHits == 0) withoutWeaponHits++;
            tested++;
            Debug.Log($"Item combat sweep RESULT: item={current} weaponHits={recordedWeaponHits} matchingItemDamageEvents={recordedItemHits} otherDamageEvents={recordedOtherHits} remaining={inventory.GetAmount(id)}");
            current=null;
        }
        Debug.Log($"Item combat sweep COMPLETE: tested={tested} failures={failed} withoutWeaponHits={withoutWeaponHits} spawns={spawns}");
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if (enemy != null && !enemy.IsBoss()) { enemy.SetDiagnosticHealth(1000f); spawns++; } }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (current == null || enemy == null || dc == null) return;
        if (dc.damageSource == EWeapon.FireStaff.ToString()) weaponHits++;
        else { otherHits++; if (dc.damageSource == current) itemHits++; }
    }
    void Unsubscribe() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
    void OnDestroy() => Unsubscribe();
}
#endif
