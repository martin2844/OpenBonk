using UnityEngine;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeWinConditions
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/Win Conditions/Escape First Crypt", order = 1)]
	public class ChallengeWinConditionEscapeFirstCrypt : ChallengeWinCondition
	{
		private ChallengeData challengeData;

		public override void Init(ChallengeData challengeData)
		{
			this.challengeData = challengeData;
			InteractableCryptLeave.A_FirstDungeonCompleted += OnCryptCompleted;
		}

		public override void Cleanup()
		{
			InteractableCryptLeave.A_FirstDungeonCompleted -= OnCryptCompleted;
		}

		private void OnCryptCompleted(float time)
		{
			if (time <= challengeData.targetValue) ChallengeCompleted();
		}
	}
}
