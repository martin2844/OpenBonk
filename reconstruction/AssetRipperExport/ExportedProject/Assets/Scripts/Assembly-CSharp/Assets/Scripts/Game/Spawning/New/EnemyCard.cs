using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Game.Spawning.New
{
	public class EnemyCard
	{
		private float costInfluenceOnWeight = 0.6f;

		public EnemyData enemy;

		public bool isElite;

		public float cost;

		public float weight;

		public EnemyCard(EnemyData enemy, bool isElite)
		{
			this.enemy = enemy;
			this.isElite = isElite;
			RefreshWeightAndCost();
		}

		public void RefreshWeightAndCost()
		{
			cost = enemy.creditCost;
			weight = 1f;
			if (isElite)
			{
				cost *= 2f;
				weight *= (PlayerStats.HasStats() ? PlayerStats.GetStat(EStat.EliteSpawnIncrease) : 1f) * 0.04f;
			}
		}

		public new string ToString()
		{
			return null;
		}
	}
}
