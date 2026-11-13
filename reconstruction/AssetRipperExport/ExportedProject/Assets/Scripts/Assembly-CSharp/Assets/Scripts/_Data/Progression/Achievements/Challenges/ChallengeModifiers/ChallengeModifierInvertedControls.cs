using UnityEngine;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/InvertedControls", order = 1)]
	public class ChallengeModifierInvertedControls : ChallengeModifier
	{
		public static bool disableInvertedControlsOptions;

		public override void Init(ChallengeData challengeData)
		{
			disableInvertedControlsOptions = true;
		}

		public override void Cleanup()
		{
		}
	}
}
