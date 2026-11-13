#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in controlled run: exercise the actual scene UI and a real altar boss.
internal sealed class AndroidBossHealthBarProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_boss_health_bar_probe", false)) return;
        var root = new GameObject("Boss health bar probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidBossHealthBarProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableBossSpawner altar = null; TargetOfInterestUi ui = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            altar = FindFirstObjectByType<InteractableBossSpawner>(); ui = FindFirstObjectByType<TargetOfInterestUi>();
            if (altar != null && ui != null && GameManager.Instance != null && GameManager.Instance.IsPlaying() && MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && EnemyManager.Instance != null && EnemyManager.Instance.summonerController != null) break;
            yield return null;
        }
        if (altar == null || ui == null || MyPlayer.Instance == null || EnemyManager.Instance == null) { Debug.LogError("Boss health bar probe: game or scene HUD unavailable"); yield break; }
        var manager = EnemyManager.Instance; bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var weapons = new List<WeaponBase>(); foreach (var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); weapons.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic); bool wasInvincible = (bool)invincible.GetValue(MyPlayer.Instance); invincible.SetValue(MyPlayer.Instance, true);
        bool activated = altar.Interact();
        var bosses = (List<Enemy>)typeof(InteractableBossSpawner).GetField("bossEnemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(altar);
        if (!activated || bosses == null || bosses.Count == 0) { Debug.LogError("Boss health bar probe: altar did not spawn a boss"); Cleanup(); yield break; }
        var boss = bosses[0]; var enemyField = typeof(TargetOfInterestPrefab).GetField("enemy", BindingFlags.Instance | BindingFlags.NonPublic);
        TargetOfInterestPrefab row = null;
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline)
        {
            row = ui.prefabs.FirstOrDefault(r => r != null && (Enemy)enemyField.GetValue(r) == boss);
            if (row != null && row.gameObject.activeInHierarchy && row.canvasGroup != null && row.canvasGroup.alpha > .95f && boss.CanTakeDamage()) break;
            yield return null;
        }
        if (row == null || !row.gameObject.activeInHierarchy || row.canvasGroup == null || row.canvasGroup.alpha <= .95f || !boss.CanTakeDamage())
        {
            Debug.LogError($"Boss health bar probe: timed out waiting for visible row and damageable boss; bound={row != null}, active={(row != null && row.gameObject.activeInHierarchy)}, alpha={(row != null && row.canvasGroup != null ? row.canvasGroup.alpha : -1f):F2}, damageable={boss.CanTakeDamage()}");
            Cleanup(); yield break;
        }
        if (row.hpBar == null || row.t_hp == null || row.t_name == null)
        {
            Debug.LogError("Boss health bar probe: visible row is missing health, name or bar references");
            Cleanup(); yield break;
        }
        float before = boss.hp, beforeScale = row.hpBar.transform.localScale.x;
        string beforeLabel = row.t_hp.text; string bossName = row.t_name.text;
        boss.DamageExternal(new DamageContainer(0f, "BossHealthBarProbe") { damage = boss.maxHp * .25f, knockback = 0f, direction = Vector3.zero });
        yield return null;
        bool passed = boss.hp < before && row.hpBar.transform.localScale.x < beforeScale && row.t_hp.text != beforeLabel && !string.IsNullOrWhiteSpace(bossName);
        string result = $"Boss health bar probe: {(passed ? "PASS" : "FAIL")}, activated={activated}, boss={boss.enemyData.enemyName}, name={bossName}, active={row.gameObject.activeInHierarchy}, alpha={row.canvasGroup.alpha:F2}, hpBefore={before:F2}, hpAfter={boss.hp:F2}, barBefore={beforeScale:F3}, barAfter={row.hpBar.transform.localScale.x:F3}, labelBefore={beforeLabel}, labelAfter={row.t_hp.text}";
        if (passed) Debug.Log(result); else Debug.LogError(result);
        if (passed)
        {
            bool checkDebuffs;
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity.Call<AndroidJavaObject>("getIntent")) checkDebuffs = intent.Call<bool>("getBooleanExtra", "megabonk_boss_status_hud_probe", false);
            if (checkDebuffs)
            {
                var source = new DamageContainer(0f, "BossStatusHudProbe") { enemy = boss };
                boss.AddDebuff(EDebuff.Poison, source, 30f, 20);
                boss.AddDebuff(EDebuff.Bloodmark, source, 30f, 50);
                deadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < deadline && (!boss.HasDebuff(EDebuff.Poison) || !boss.HasDebuff(EDebuff.Bloodmark))) yield return null;
                yield return new WaitForSecondsRealtime(2f);
                float hpScale = row.hpBar.transform.localScale.x;
                float poisonScale = row.poison != null ? row.poison.transform.localScale.x : -1f;
                float bloodScale = row.bloodmark != null ? row.bloodmark.transform.localScale.x : -1f;
                bool debuffsOk = boss.HasDebuff(EDebuff.Poison) && boss.HasDebuff(EDebuff.Bloodmark) && poisonScale > 0f && bloodScale > 0f && poisonScale + bloodScale <= hpScale + .001f;
                string debuffResult = $"Boss status HUD probe: {(debuffsOk ? "PASS" : "FAIL")}, poison={poisonScale:F3}, bloodmark={bloodScale:F3}, hp={hpScale:F3}";
                if (debuffsOk) Debug.Log(debuffResult); else Debug.LogError(debuffResult);
                yield return new WaitForSecondsRealtime(5f);
                boss.QueueClearAllDebuffs();
            }
            // Keep weapons disabled during this observation: otherwise the boss can
            // die after the initial sample and its disappearing row is expected.
            float start = Time.realtimeSinceStartup;
            int samples = 0; bool alive = true; bool visible = true;
            string detail = string.Empty;
            while (Time.realtimeSinceStartup - start < 20f)
            {
                yield return null;
                if (boss == null || boss.IsDeadOrDyingNextFrame() || !boss.gameObject.activeInHierarchy)
                {
                    alive = false; detail = "boss died or was released during observation"; break;
                }
                row = ui != null ? ui.prefabs.FirstOrDefault(r => r != null && (Enemy)enemyField.GetValue(r) == boss) : null;
                Canvas parentCanvas = row != null ? row.GetComponentInParent<Canvas>() : null;
                if (row == null || !row.gameObject.activeInHierarchy || row.canvasGroup == null || row.canvasGroup.alpha <= .95f || parentCanvas == null || !parentCanvas.isActiveAndEnabled || row.hpBar == null || !row.hpBar.isActiveAndEnabled || row.hpBar.color.a <= .01f || row.hpBar.transform.localScale.x <= 0f || EffectiveAlpha(row.hpBar.transform) <= .01f)
                {
                    visible = false;
                    detail = $"rowBound={row != null}, rowActive={(row != null && row.gameObject.activeInHierarchy)}, alpha={(row != null && row.canvasGroup != null ? row.canvasGroup.alpha : -1f):F2}, hp={boss.hp:F2}";
                    break;
                }
                samples++;
            }
            string persistence = $"Boss health bar persistence: {(!alive ? "INCONCLUSIVE" : visible ? "PASS" : "FAIL")}, seconds={Time.realtimeSinceStartup - start:F2}, samples={samples}, bossHp={(boss != null ? boss.hp : -1f):F2}, {detail}";
            if (alive && !visible) Debug.LogError(persistence); else Debug.Log(persistence);
        }
        Cleanup();
        void Cleanup() { if (MyPlayer.Instance != null) invincible.SetValue(MyPlayer.Instance, wasInvincible); if (manager != null) manager.enabledWaves = waves; foreach (var weapon in weapons) if (weapon != null) weapon.Enable(); }
    }

    private static float EffectiveAlpha(Transform leaf)
    {
        float alpha = 1f;
        for (Transform current = leaf; current != null; current = current.parent)
        {
            bool stop = false;
            foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
            {
                if (!group.isActiveAndEnabled) continue;
                alpha *= group.alpha;
                stop |= group.ignoreParentGroups;
            }
            if (stop) break;
        }
        return alpha;
    }
}
#endif
