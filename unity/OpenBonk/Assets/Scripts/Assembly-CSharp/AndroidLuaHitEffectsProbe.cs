#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaHitEffectsProbe : MonoBehaviour
{
    private bool allEffects;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_hit_effects_probe", false)) return;
        var go = new GameObject("Lua hit effects probe"); DontDestroyOnLoad(go);
        go.AddComponent<AndroidLuaHitEffectsProbe>().allEffects = intent.Call<bool>("getBooleanExtra", "openbonk_all_hit_effects_probe", false);
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var id = (EItem)OpenBonkContentIds.Item("openbonk-item-on-hit", "training-cell");
        if (player == null || player.inventory == null || EnemyManager.Instance == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData data) || !data.luaOnHit) { Debug.LogError("Lua hit effects INCONCLUSIVE: item or run unavailable"); yield break; }
        var manager = EnemyManager.Instance; manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        foreach (var old in new List<Enemy>(manager.enemies.Values)) if (old != null && old.gameObject.activeInHierarchy) old.ReleaseToPool();
        var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -905, true, canBeElite: false);
        end = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < end && target != null && !target.CanTakeDamage()) yield return null;
        if (target == null || !target.CanTakeDamage()) { Debug.LogError("Lua hit effects INCONCLUSIVE: target unavailable"); yield break; }
        target.enemyMovement.StopMovement(); target.SetDiagnosticHealth(1000f);
        var inv = player.inventory.itemInventory; inv.AddItem(id);
        float bonus = 0f; int bonusEvents = 0;
        void Record(Enemy enemy, DamageContainer dc) { if (enemy == target && dc.damageSource == "mod:openbonk-item-on-hit:training-cell") { bonus += dc.damage; bonusEvents++; } }
        Enemy.A_Damage += Record;
        try {
            var hit = new DamageContainer(1f, "probe") { enemy = target, damage = 10f };
            target.DamageFromPlayerWeapon(hit);
            bool queued = target.debuffsToAdd.ContainsKey(EDebuff.Freeze) && target.debuffsToAdd.ContainsKey(EDebuff.Poison) &&
                target.debuffsToAdd.ContainsKey(EDebuff.Stun) == allEffects && target.debuffsToAdd.ContainsKey(EDebuff.Burn) == allEffects;
            yield return new WaitForSecondsRealtime(.4f);
            bool applied = target.HasDebuff(EDebuff.Freeze) && target.HasDebuff(EDebuff.Poison) &&
                (!allEffects || (target.HasDebuff(EDebuff.Stun) && target.HasDebuff(EDebuff.Burn)));
            bool normal = queued && applied && bonusEvents == 1 && Math.Abs(bonus - 2.5f) < .01f;
            inv.AddItem(id);
            target.ClearAllDebuffs();
            hit = new DamageContainer(1f, "probe") { enemy = target, damage = 10f };
            target.DamageFromPlayerWeapon(hit);
            bool atomic = bonusEvents == 1 && target.debuffsToAdd.Count == 0;
            Debug.Log($"Lua hit effects {(normal && atomic ? "PASS" : "FAIL")}: allEffects={allEffects} queued={queued} applied={applied} bonus={bonus:F3} bonusEvents={bonusEvents} invalidAtomic={atomic}");
        } finally { Enemy.A_Damage -= Record; }
    }
}
#endif
