#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

// Opt-in emulator check of controlled chest awards and the enemy damage path.
internal sealed class AndroidExternalItemChestProbe : MonoBehaviour
{
    private Enemy target;
    private float observed = -1f;
    private bool contextMode;
    private bool attackOnly;
    private bool onHitMode;
    private float observedBonus;
    private int bonusEvents;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_item_chest_probe", false)) return;
        var go = new GameObject("External item chest probe"); DontDestroyOnLoad(go);
        var probe = go.AddComponent<AndroidExternalItemChestProbe>();
        probe.contextMode = intent.Call<bool>("getBooleanExtra", "openbonk_item_context_probe", false);
        probe.attackOnly = intent.Call<bool>("getBooleanExtra", "openbonk_item_attack_only_probe", false);
        probe.onHitMode = intent.Call<bool>("getBooleanExtra", "openbonk_item_on_hit_probe", false);
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 100f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || EnemyManager.Instance == null || RunUnlockables.availableItems == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item(onHitMode ? "openbonk-item-on-hit" : attackOnly ? "openbonk-item-attack-only" : contextMode ? "openbonk-item-context-fixture" : "openbonk-item-lua-fixture", "training-cell");
        if (player == null || manager == null || player.inventory == null || !DataManager.Instance.TryGetItem(id, out var item) || !RunUnlockables.availableItems[item.rarity].Contains(item) || player.inventory.itemInventory.GetAmount(id) != 0)
        { Debug.LogError("External item chest probe INCONCLUSIVE: missing run, registered item or empty inventory"); Destroy(gameObject); yield break; }
        WeaponBase weapon = null; var disabled = new List<WeaponBase>();
        foreach (var candidate in player.inventory.weaponInventory.weapons.Values)
        {
            if (weapon == null) weapon = candidate;
            if (candidate.enabled) { candidate.Disable(); disabled.Add(candidate); }
        }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        Enemy boss = contextMode ? manager.SpawnBoss(EEnemy.Ent1, -1, EEnemyFlag.Boss, player.transform.position + Vector3.left * 12f) : null;
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && target != null && (!target.CanTakeDamage() || (contextMode && boss != null && !boss.CanTakeDamage()))) yield return null;
        if (weapon == null || target == null || !target.CanTakeDamage() || (contextMode && (boss == null || !boss.CanTakeDamage() || !boss.IsBoss())))
        {
            foreach (var candidate in disabled) candidate.Enable(); manager.enabledWaves = waves;
            Debug.LogError("External item chest probe INCONCLUSIVE: target or weapon unavailable"); Destroy(gameObject); yield break;
        }
        target.enemyMovement.StopMovement(); target.SetDiagnosticHealth(1000f);
        if (boss != null) { boss.enemyMovement.StopMovement(); boss.SetDiagnosticHealth(1000f); }
        var snapshots = new Dictionary<EItemRarity, List<ItemData>>();
        foreach (var pair in RunUnlockables.availableItems) snapshots.Add(pair.Key, new List<ItemData>(pair.Value));
        Enemy.A_Damage += RecordDamage;
        try
        {
            float baseline = Hit(weapon);
            float baselineBonus = observedBonus;
            var goblin = target; float bossBaseline = 0f, bossWithItem = 0f;
            if (contextMode) { target = boss; bossBaseline = Hit(weapon); target = goblin; }
            foreach (var pair in RunUnlockables.availableItems) { pair.Value.Clear(); pair.Value.Add(item); }
            ChestUtility.OpenChestNoAnimation(EChest.Normal);
            bool first = player.inventory.itemInventory.GetAmount(id) == 1;
            float once = Hit(weapon);
            float bonusOnce = observedBonus;
            bool ignoredInvalidHits = true;
            if (onHitMode)
            {
                int before = bonusEvents;
                target.MakeInvulnerable(true);
                target.DamageFromPlayerWeapon(new DamageContainer(1f, "probe-invulnerable") { damage = 1f, enemy = target });
                target.MakeInvulnerable(false);
                target.DamageFromPlayerWeapon(new DamageContainer(0f, "probe-no-proc") { damage = 1f, enemy = target });
                ignoredInvalidHits = bonusEvents == before;
            }
            ChestUtility.OpenChestNoAnimation(EChest.Normal);
            bool second = player.inventory.itemInventory.GetAmount(id) == 2;
            float twice = Hit(weapon);
            float bonusTwice = observedBonus;
            bool damageRow = true;
            if (onHitMode)
            {
                damageRow = false;
                string sourceId = "mod:openbonk-item-on-hit:training-cell";
                if (RunStats.damageSources != null && RunStats.damageSources.TryGetValue(sourceId, out var source))
                {
                    foreach (var panel in Resources.FindObjectsOfTypeAll<GameOverDamageSourcesUi>())
                    {
                        if (panel == null || !panel.gameObject.scene.IsValid() || panel.damageSourcePrefab == null) continue;
                        var rowObject = Instantiate(panel.damageSourcePrefab); var row = rowObject.GetComponent<DamageSourceEntry>();
                        if (row != null)
                        {
                            row.Set(source);
                            damageRow = row.icon != null && row.icon.texture == item.GetIcon() && row.t_sourceName != null && row.t_sourceName.text == item.GetName() &&
                                row.t_lvl != null && row.t_lvl.text == "2" && Near(source.damage, bonusOnce + bonusTwice);
                        }
                        Destroy(rowObject); break;
                    }
                }
            }
            if (contextMode) { target = boss; bossWithItem = Hit(weapon); target = goblin; }
            player.inventory.itemInventory.RemoveItem(id); player.inventory.itemInventory.RemoveItem(id);
            float restored = Hit(weapon);
            float restoredBonus = observedBonus;
            bool pass = first && second && baseline > 0f && Near(restored / baseline, 1f) &&
                (onHitMode ? damageRow && ignoredInvalidHits && Near(once / baseline, 1f) && Near(twice / baseline, 1f) && Near(baselineBonus, 0f) && Near(bonusOnce / baseline, .25f) && Near(bonusTwice / baseline, .5f) && Near(restoredBonus, 0f) && bonusEvents == 2
                    : Near(once / baseline, 1.25f) && Near(twice / baseline, 1.5f)) && (!contextMode || (bossBaseline > 0f && Near(bossWithItem / bossBaseline, 2f)));
            Debug.Log($"External item chest probe {(pass ? "PASS" : "FAIL")}: context={contextMode} attackOnly={attackOnly} onHit={onHitMode} damageRow={damageRow} ignoredInvalid={ignoredInvalidHits} chestStacks={first}/{second} damage={baseline:F3}/{once:F3}/{twice:F3}/{restored:F3} bonuses={baselineBonus:F3}/{bonusOnce:F3}/{bonusTwice:F3}/{restoredBonus:F3} bonusEvents={bonusEvents} boss={bossBaseline:F3}/{bossWithItem:F3}");
        }
        catch (Exception e) { Debug.LogError("External item chest probe FAIL: " + e); }
        finally
        {
            Enemy.A_Damage -= RecordDamage;
            foreach (var pair in snapshots) { RunUnlockables.availableItems[pair.Key].Clear(); RunUnlockables.availableItems[pair.Key].AddRange(pair.Value); }
            foreach (var candidate in disabled) candidate.Enable(); manager.enabledWaves = waves;
        }
        Destroy(gameObject);
    }
    private float Hit(WeaponBase weapon)
    {
        DamageContainer damage = null;
        for (int i = 0; i < 30; i++)
        {
            damage = WeaponUtility.GetDamageContainer(weapon, null, target, Vector3.right, 1f);
            if (!damage.crit) break;
        }
        if (damage == null || damage.crit) throw new InvalidOperationException("could not obtain noncritical hit");
        observed = -1f; observedBonus = 0f; target.DamageFromPlayerWeapon(damage);
        if (observed < 0f) throw new InvalidOperationException("enemy did not receive damage");
        return observed;
    }
    private void RecordDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy != target || damage == null) return;
        if (onHitMode && damage.damageSource == "mod:openbonk-item-on-hit:training-cell") { observedBonus += damage.damage; bonusEvents++; }
        else observed = damage.damage;
    }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .001f;
}
#endif
