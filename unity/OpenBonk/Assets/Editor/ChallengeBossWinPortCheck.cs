using System;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeWinConditions;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using UnityEngine;

public static class ChallengeBossWinPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("ChallengeBossWinPortCheck: " + message); }
    public static void Run()
    {
        var previousConfig = MapController.runConfig; int previousIndex = MapController.index;
        var previousStage = InteractableBossSpawner.A_BossDefeated; var previousFinal = FinalFightController.A_BossDefeated; var previousGhost = GraveyardBossRoom.A_BossDied;
        var condition = ScriptableObject.CreateInstance<ChallengeWinConditionKillTierBoss>();
        try
        {
            InteractableBossSpawner.A_BossDefeated = null; FinalFightController.A_BossDefeated = null; GraveyardBossRoom.A_BossDied = null;
            MapController.runConfig = new RunConfig { mapTierIndex = 1 }; MapController.index = 0;
            condition.Init(null);
            Require(InteractableBossSpawner.A_BossDefeated != null && FinalFightController.A_BossDefeated != null && GraveyardBossRoom.A_BossDied != null, "all three original boss events subscribed");
            InteractableBossSpawner.A_BossDefeated(false); Require(!condition.completed, "intermediate boss does not win tier");
            MapController.index = 1; InteractableBossSpawner.A_BossDefeated(false); Require(condition.completed, "tier-final stage boss completes condition");
            condition.Cleanup();
            Require(InteractableBossSpawner.A_BossDefeated == null && FinalFightController.A_BossDefeated == null && GraveyardBossRoom.A_BossDied == null, "cleanup removes all three callbacks");
            Debug.Log("ChallengeBossWinPortCheck passed: normal stage boss completes only at final stage and all callbacks unsubscribe");
        }
        finally
        {
            condition.Cleanup(); UnityEngine.Object.DestroyImmediate(condition); MapController.runConfig = previousConfig; MapController.index = previousIndex;
            InteractableBossSpawner.A_BossDefeated = previousStage; FinalFightController.A_BossDefeated = previousFinal; GraveyardBossRoom.A_BossDied = previousGhost;
        }
    }
}
