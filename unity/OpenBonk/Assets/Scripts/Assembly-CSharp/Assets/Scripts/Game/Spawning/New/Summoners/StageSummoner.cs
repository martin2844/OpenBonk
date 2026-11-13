using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEngine;

namespace Assets.Scripts.Game.Spawning.New.Summoners
{
	public class StageSummoner : BaseSummoner
	{
		private List<EEnemy> enemyPool = new List<EEnemy>();

		private float capReduction = 0.25f;
		public float MapSpawnMultiplier { get; private set; } = 1f;

		public bool AddEnemyToPoolAndPickNewCard(List<EEnemy> eEnemy, out EEnemy selectedEnemy)
		{
			selectedEnemy = default(EEnemy);
			float budget = GetBaseCreditsPerSecond() * GetMultiplier() * GetSummonInterval();
			var eligible = new List<EEnemy>();
			foreach (EEnemy enemy in enemyPool)
			{
				EnemyData data = DataManager.Instance.GetEnemyData(enemy);
				if (data.creditCost <= budget && MyTime.stageTimer > data.canSpawnAfterTime) eligible.Add(enemy);
			}
			if (eligible.Count == 0) return false;
			selectedEnemy = eligible[Random.Range(0, eligible.Count)];
			enemyPool.Remove(selectedEnemy);
			currentEnemies.Add(selectedEnemy);
			GenerateCardsForSummoner(currentEnemies);
			return true;
		}

		protected override void Init()
		{
		}

		protected override List<EEnemy> GetEnemies()
		{
			return currentEnemies;
		}

		public override float GetSummonInterval()
		{
			return 2f * Mathf.Clamp(1f - 0.5f * PlayerStats.GetStat(EStat.Difficulty), 0.1f, 2f);
		}

		public float GetBaseCreditsPerSecondUncapped()
		{
			return (1f + MyTime.stageTimer / 60f * 0.5f) * PlayerStats.GetStat(EStat.EnemyAmountMultiplier) * MapSpawnMultiplier;
		}

		public override float GetBaseCreditsPerSecond()
		{
			if (!IsGainingCredits()) return 0f;
			float rate = GetBaseCreditsPerSecondUncapped();
			return IsTargetEnemiesReached() ? rate * capReduction : rate;
		}

		public override float GetInitialCredits()
		{
			return 0f;
		}

		public override int GetNumTargetEnemies()
		{
			float progress = Mathf.Clamp01(MyTime.stageTimer / MapController.currentMap.stageDuration);
			return Mathf.RoundToInt((5f + 200f * progress) * GetMultiplier() * PlayerStats.GetStat(EStat.EnemyAmountMultiplier) * MapSpawnMultiplier);
		}

		protected override bool UseDirectionBias()
		{
			return GameManager.Instance == null || !GameManager.Instance.isCrypt;
		}

		protected override bool ForceSpawn()
		{
			return false;
		}

		private bool IsGainingCredits()
		{
			if (MapController.currentMap.eMap != EMap.Graveyard) return true;
			if (GameManager.Instance.isCrypt && !GameManager.Instance.isDungeonTimerStarted) return false;
			if (RsgController.Instance != null && RsgController.Instance.roomBoss.isFightingBoss) return false;
			return !GameManager.Instance.isCrypt || (!GameManager.Instance.isDungeonOvertime &&
				!ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_CRYPT));
		}

		private bool IsTargetEnemiesReached()
		{
			return EnemyManager.Instance.GetEnemiesFromSummoner(id) >= GetNumTargetEnemies();
		}

		public StageSummoner(int id, List<EEnemy> defaultEnemies)
			: base(id, defaultEnemies)
		{
			MapSpawnMultiplier = OpenBonkMapLuaHooks.StageSpawnMultiplier(MapController.currentMap, MapController.index + 1);
			EMap map = OpenBonkExternalMapData.EnemyPoolMap(MapController.currentMap);
			foreach (EnemyData data in DataManager.Instance.unsortedEnemies)
				if ((data.maps & map) == map && data.minStage <= MapController.GetStageIndex() && !defaultEnemies.Contains(data.enemyName))
					enemyPool.Add(data.enemyName);
		}
	}
}
