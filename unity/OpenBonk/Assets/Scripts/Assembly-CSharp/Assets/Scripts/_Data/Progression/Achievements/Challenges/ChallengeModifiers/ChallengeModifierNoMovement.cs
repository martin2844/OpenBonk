using UnityEngine;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/No movement", order = 1)]
	public class ChallengeModifierNoMovement : ChallengeModifier
	{
		public override void Init(ChallengeData challengeData)
		{
		}

		public override void Cleanup()
		{
		}
	}
}
