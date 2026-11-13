using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Spawning.New.Summoners
{
	public class ChallengeSummoner : BaseSummoner
	{
		private List<EEnemy> enemyPool;

		private Vector3 pos;

		public ChallengeSummoner(int id, List<EEnemy> defaultEnemies)
			: base(id, defaultEnemies)
		{
		}

		protected override void Init()
		{
			enemyPool = new List<EEnemy>();
			float maximumCost = GetInitialCredits() * .5f;
			var candidates = new List<EEnemy>();
			var map = OpenBonkExternalMapData.EnemyPoolMap(MapController.currentMap);
			foreach (EnemyData data in DataManager.Instance.unsortedEnemies)
				if ((data.maps & map) == map && data.minStage <= MapController.index && data.creditCost <= maximumCost)
					candidates.Add(data.enemyName);
			if (candidates.Count > 0) enemyPool.Add(candidates[Random.Range(0, candidates.Count)]);
		}

		public List<Enemy> SpawnEnemies(bool onlyElites, Vector3 position)
		{
			pos = position;
			return SpendCredits(!onlyElites);
		}

		public override List<Enemy> SpendCredits(bool useWeights = true)
		{
			var spawned = new List<Enemy>();
			while (credits > 0f)
			{
				EnemyCard card = GetRandomCard(useWeights);
				if (card == null) break;
				credits -= card.cost;
				Vector3 spawnPos = SpawnPositions.GetEnemySpawnPositionAroundPoint(pos, 0f, 8f, 50);
				if ((spawnPos - SpawnPositions.INVALID_POS).sqrMagnitude < 1e-10f) spawnPos = pos;
				var flags = EEnemyFlag.Challenge | (card.isElite ? EEnemyFlag.Elite : EEnemyFlag.None);
				spawned.Add(EnemyManager.Instance.SpawnEnemy(card.enemy, spawnPos, id, true, flags, true, 1f));
			}
			return spawned;
		}

		protected override List<EEnemy> GetEnemies()
		{
			return enemyPool;
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
			return PlayerStats.GetStat(EStat.EnemyAmountMultiplier) * (1f + MyTime.stageTimer / 60f * .5f) * Random.Range(3f, 5f);
		}

		public override int GetNumTargetEnemies()
		{
			return 999;
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
