using UnityEngine;
using Assets.Scripts.Actors.Player;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/Blind", order = 1)]
	public class ChallengeModifierBlind : ChallengeModifier
	{
		public override void Init(ChallengeData challengeData)
		{
			MapGenerationController.A_GenerationComplete += OnGenerationComplete;
		}

		public override void Cleanup()
		{
			MapGenerationController.A_GenerationComplete -= OnGenerationComplete;
		}

		private void OnGenerationComplete()
		{
			Object.Instantiate(EffectManager.Instance.blindSphere, MyPlayer.Instance.transform.position, Quaternion.identity);
			RenderSettings.fogDensity = 0.035f;
			RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, Color.black, 0.5f);
		}
	}
}
