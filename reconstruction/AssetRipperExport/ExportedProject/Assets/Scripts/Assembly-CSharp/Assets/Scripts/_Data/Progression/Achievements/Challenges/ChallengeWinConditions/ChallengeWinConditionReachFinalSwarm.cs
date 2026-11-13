using UnityEngine;
using Assets.Scripts.Game.Spawning.New;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeWinConditions
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/Win Conditions/Reach Final Swarm", order = 1)]
	public class ChallengeWinConditionReachFinalSwarm : ChallengeWinCondition
	{
		public override void Init(ChallengeData challengeData)
		{
			SummonerController.A_FinalSwarmStarted += ChallengeCompleted;
		}

		public override void Cleanup()
		{
			SummonerController.A_FinalSwarmStarted -= ChallengeCompleted;
		}
	}
}
