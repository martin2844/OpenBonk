using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Spawning.New.Summoners
{
	public class SwarmSummoner : BaseSummoner
	{
		private List<EEnemy> finalSwarmEnemies = new List<EEnemy> { EEnemy.Ghost };

		public const float swarmSummonInterval = 0.5f;

		private int ghostTier;

		private bool hasSwappedEnemy;

		private List<Enemy> spawnedEnemies;

		private float swarmHpMultiplier = 1.3f;

		private float swarmSpeedMultiplier = 1.2f;

		protected override void Init()
		{
		}

		protected override void TickExtra()
		{
			if (hasSwappedEnemy) return;
			if (MyTime.finalSwarmTimer > 60f && ghostTier == 0) { GenerateCardsForSummoner(new List<EEnemy> { EEnemy.GreaterGhost }); ghostTier = 1; }
			if (MyTime.finalSwarmTimer > 360f && ghostTier == 1) { GenerateCardsForSummoner(new List<EEnemy> { EEnemy.GhostPurple }); ghostTier = 2; }
			if (MyTime.finalSwarmTimer > 720f && ghostTier == 2) { GenerateCardsForSummoner(new List<EEnemy> { EEnemy.GhostRed }); ghostTier = 3; }
		}

		protected override List<EEnemy> GetEnemies()
		{
			return GameManager.Instance.IsFinalSwarm() ? finalSwarmEnemies : currentEnemies;
		}

		public override float GetSummonInterval()
		{
			return swarmSummonInterval;
		}

		public override float GetBaseCreditsPerSecond()
		{
			float total = 0f;
			foreach (EEnemy enemy in currentEnemies) total += DataManager.Instance.GetEnemyData(enemy).creditCost;
			return currentEnemies.Count == 0 ? 0f : total / currentEnemies.Count * GetNumTargetEnemies() * 0.05f;
		}

		public override float GetInitialCredits()
		{
			return 0f;
		}

		public override int GetNumTargetEnemies()
		{
			if (GameManager.Instance.IsFinalSwarm())
				return Mathf.RoundToInt((50f + Mathf.Clamp(MyTime.finalSwarmTimer * 2.5f, 0f, EnemyManager.Instance.GetNumMaxEnemies())) * GetMultiplier());
			return Mathf.RoundToInt((240f + 65f * MapController.GetStageIndex()) * (1f + PlayerStats.GetStat(EStat.Difficulty)));
		}

		protected override bool UseDirectionBias()
		{
			return true;
		}

		public override List<Enemy> SpendCredits(bool useWeights = true)
		{
			spawnedEnemies = base.SpendCredits(useWeights);
			foreach (Enemy enemy in spawnedEnemies)
			{
				if (enemy == null) continue;
				enemy.SetSwarmMultiplierHp(swarmHpMultiplier);
				enemy.SetSpeedMultiplier(swarmSpeedMultiplier);
			}
			return spawnedEnemies;
		}

		protected override bool ForceSpawn()
		{
			return false;
		}

		public SwarmSummoner(int id, List<EEnemy> defaultEnemies)
			: base(id, defaultEnemies)
		{
		}
	}
}
