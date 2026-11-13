using System.Collections.Generic;
using Actors.Enemies;

namespace Assets.Scripts.Game.Spawning.New.Summoners
{
	public class BossSummoner : BaseSummoner
	{
		public BossSummoner(int id, List<EEnemy> defaultEnemies)
			: base(id, defaultEnemies)
		{
		}

		protected override void Init()
		{
		}

		protected override List<EEnemy> GetEnemies()
		{
			return null;
		}

		public override float GetSummonInterval()
		{
			return 0f;
		}

		public override float GetBaseCreditsPerSecond()
		{
			return 0f;
		}

		public override float GetInitialCredits()
		{
			return 0f;
		}

		public override int GetNumTargetEnemies()
		{
			return 0;
		}

		protected override bool UseDirectionBias()
		{
			return false;
		}

		protected override bool ForceSpawn()
		{
			return true;
		}
	}
}
