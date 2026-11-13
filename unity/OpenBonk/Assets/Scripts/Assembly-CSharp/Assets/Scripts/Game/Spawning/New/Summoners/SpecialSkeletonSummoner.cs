using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Game.Spawning.New.Summoners
{
	public class SpecialSkeletonSummoner : BaseSummoner
	{
		public SpecialSkeletonSummoner(int id, List<EEnemy> defaultEnemies)
			: base(id, defaultEnemies)
		{
		}

		protected override void Init()
		{
		}

		protected override List<EEnemy> GetEnemies()
		{
			return new List<EEnemy> { EEnemy.GoldenSkeleton, EEnemy.XpSkeleton };
		}

		public override List<Enemy> SpendCredits(bool useWeights = true)
		{
			EnemyCard card = GetRandomCard(true);
			if (card != null) EnemyManager.Instance.SpawnEnemy(card.enemy, id, true);
			else MyLogger.LogError("Special skeleton summoner has no affordable card");
			return null;
		}

		public override float GetSummonInterval()
		{
			return 100f;
		}

		public override float GetBaseCreditsPerSecond()
		{
			return 0f;
		}

		public override float GetInitialCredits()
		{
			return 1000f;
		}

		public override int GetNumTargetEnemies()
		{
			return 1;
		}

		protected override bool UseDirectionBias()
		{
			return false;
		}

		protected override bool ForceSpawn()
		{
			return true;
		}

		protected override bool UseMultiplier()
		{
			return false;
		}
	}
}
