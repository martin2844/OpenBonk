#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeWinConditions;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in controlled challenge and boss damage. The boss death and achievement events use real game paths.
internal sealed class AndroidTierChallengeBossProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_tier_challenge_boss_probe", false)) return;
        var go = new GameObject("Tier challenge boss probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidTierChallengeBossProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableBossSpawner altar = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            altar = FindFirstObjectByType<InteractableBossSpawner>();
            if (altar != null && GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && MyPlayer.Instance.inventory != null && EnemyManager.Instance != null && EnemyManager.Instance.summonerController != null && DataManager.Instance != null && SaveManager.loaded && MapController.runConfig != null && PoolManager.Instance != null) break;
            yield return null;
        }
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var challenge = DataManager.Instance != null ? DataManager.Instance.GetAchievement("a_forest1_turbo") as ChallengeData : null;
        if (altar == null || player == null || manager == null || challenge == null || !(challenge.winCondition is ChallengeWinConditionKillTierBoss) || MapController.runConfig == null) { Debug.LogError("Tier challenge boss probe: scene or challenge unavailable"); yield break; }
        bool oldWaves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var until = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); float oldUntil = (float)until.GetValue(player);
        invincible.SetValue(player, true); until.SetValue(player, MyTime.time + 10000f);
        var config = MapController.runConfig; var oldChallenge = config.challenge; int oldTier = config.mapTierIndex;
        config.challenge = challenge; config.mapTierIndex = MapController.index;
        var condition = Instantiate(challenge.winCondition) as ChallengeWinConditionKillTierBoss;
        condition.Init(challenge);
        int bossDefeats = 0, completionEvents = 0, unlockEvents = 0;
        bool unlockedBefore = MyAchievements.IsUnlocked(challenge);
        InteractableBossSpawner.A_BossDefeated += OnBoss; ChallengesTracker.A_ChallengeCompleted += OnChallenge; MyAchievements.A_Unlocked += OnUnlock;
        altar.transform.position = player.transform.position + player.transform.forward * 3f;
        bool activated = altar.Interact();
        var bosses = (List<Enemy>)typeof(InteractableBossSpawner).GetField("bossEnemies", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(altar);
        Debug.Log($"Tier challenge boss probe: challenge={challenge.internalName}, tier={config.mapTierIndex}, stage={MapController.index}, unlockedBefore={unlockedBefore}, directAltar={activated}, bosses={(bosses != null ? bosses.Count : 0)}");
        if (bosses != null)
        {
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && bosses.Exists(e => e != null && !e.CanTakeDamage())) yield return null;
            foreach (var enemy in new List<Enemy>(bosses)) if (enemy != null && enemy.CanTakeDamage()) enemy.Kill("TierChallengeProbe");
        }
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && bossDefeats == 0) yield return null;
        Debug.Log($"Tier challenge boss probe: finished forcedBossKill=true, bossDefeats={bossDefeats}, conditionCompleted={condition.completed}, challengeEvents={completionEvents}, unlockEvents={unlockEvents}, unlockedAfter={MyAchievements.IsUnlocked(challenge)}");
        InteractableBossSpawner.A_BossDefeated -= OnBoss; ChallengesTracker.A_ChallengeCompleted -= OnChallenge; MyAchievements.A_Unlocked -= OnUnlock;
        condition.Cleanup(); Destroy(condition); config.challenge = oldChallenge; config.mapTierIndex = oldTier;
        if (player != null) { invincible.SetValue(player, oldInvincible); until.SetValue(player, oldUntil); } if (manager != null) manager.enabledWaves = oldWaves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
        void OnBoss(bool portal) { bossDefeats++; }
        void OnChallenge(ChallengeData data) { if (data == challenge) completionEvents++; }
        void OnUnlock(MyAchievement data) { if (data == challenge) unlockEvents++; }
    }
}
#endif
