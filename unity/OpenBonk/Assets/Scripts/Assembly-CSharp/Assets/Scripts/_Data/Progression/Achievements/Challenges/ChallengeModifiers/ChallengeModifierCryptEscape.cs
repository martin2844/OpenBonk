using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/Crypt", order = 1)]
	public class ChallengeModifierCryptEscape : ChallengeModifier
	{
		private ChallengeData challengeData;

		private float timeLimit;

		public override void Init(ChallengeData challengeData)
		{
			this.challengeData = challengeData;
			timeLimit = challengeData != null ? challengeData.targetValue : 0f;
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
			if (GameManager.Instance == null || GameManager.Instance.cryptIndex != 0 || MyTime.cryptTimer < timeLimit) return;
			var health = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null ? MyPlayer.Instance.inventory.playerHealth : null;
			if (health != null) health.KillPlayer();
		}
	}
}
