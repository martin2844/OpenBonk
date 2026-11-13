using UnityEngine;
using Assets.Scripts.Managers;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeWinConditions
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/Win Conditions/Kill Tier Boss", order = 1)]
	public class ChallengeWinConditionKillTierBoss : ChallengeWinCondition
	{
		public override void Init(ChallengeData challengeData)
		{
			InteractableBossSpawner.A_BossDefeated += OnStageBossDied;
			FinalFightController.A_BossDefeated += OnStageBossDied;
			GraveyardBossRoom.A_BossDied += OnGhostBossDied;
		}

		public override void Cleanup()
		{
			InteractableBossSpawner.A_BossDefeated -= OnStageBossDied;
			FinalFightController.A_BossDefeated -= OnStageBossDied;
			GraveyardBossRoom.A_BossDied -= OnGhostBossDied;
		}

		private void OnStageBossDied(bool didPortalOpen)
		{
			if (MapController.IsTierFinalStage()) ChallengeCompleted();
		}

		private void OnGhostBossDied()
		{
			if (MapController.IsTierFinalStage()) ChallengeCompleted();
		}
	}
}
