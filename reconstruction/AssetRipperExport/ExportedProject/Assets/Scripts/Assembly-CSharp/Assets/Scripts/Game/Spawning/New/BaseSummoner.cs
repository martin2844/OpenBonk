using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using Utility;

namespace Assets.Scripts.Game.Spawning.New
{
	public abstract class BaseSummoner
	{
		protected float credits;

		private List<EnemyCard> cards;

		private float giveCreditsTimer;

		private float spendCreditsTimer;

		public const int maxEnemiesPerSecond = 500;

		public const int maxEnemiesPerCycle = 200;

		protected int enemiesThisSecond;

		private int nextSecond;

		protected int id;

		public bool slowmode;

		private float slowmodeMultiplier = 0.5f;

		private float slowmodeOverAtTime;

		private bool isWaveMode;

		private float waveModeOverAtTime;

		private List<EnemyCard> waveModeCards;

		protected List<EEnemy> currentEnemies;

		private List<Enemy> spawnedEnemies = new List<Enemy>();

		public BaseSummoner(int id, List<EEnemy> defaultEnemies)
		{
			Init();
			currentEnemies = new List<EEnemy>(defaultEnemies);
			this.id = id;
			credits = GetInitialCredits();
			cards = GenerateCards(GetEnemies());
			PlayerStatsNew.A_StatUpdate += OnStatUpdated;
			MyPlayer.A_PlayerInventoryInitialized += OnPlayerInventoryInitialized;
		}

		public void Cleanup()
		{
			PlayerStatsNew.A_StatUpdate -= OnStatUpdated;
			MyPlayer.A_PlayerInventoryInitialized -= OnPlayerInventoryInitialized;
		}

		public void Tick()
		{
			if (isWaveMode && MyTime.time >= waveModeOverAtTime) isWaveMode = false;
			float multiplier = 1f;
			if (slowmode)
			{
				if (MyTime.time >= slowmodeOverAtTime) slowmode = false;
				else multiplier = slowmodeMultiplier;
			}
			float elapsed = MyTime.fixedDeltaTime * multiplier;
			giveCreditsTimer += elapsed;
			spendCreditsTimer += elapsed;
			if (giveCreditsTimer >= 1f) { AddCredits(); giveCreditsTimer = 0f; }
			if (spendCreditsTimer >= GetSummonInterval()) { SpendCredits(); spendCreditsTimer = 0f; }
			if (MyTime.time > nextSecond) { nextSecond = Mathf.CeilToInt(MyTime.time) + 1; enemiesThisSecond = 0; }
			TickExtra();
		}

		protected virtual void TickExtra()
		{
		}

		protected void GenerateCardsForSummoner(List<EEnemy> enemies)
		{
			cards = GenerateCards(enemies);
		}

		protected virtual List<EnemyCard> GenerateCards(List<EEnemy> enemies)
		{
			var result = new List<EnemyCard>();
			foreach (EEnemy name in enemies)
			{
				EnemyData data = DataManager.Instance.GetEnemyData(name);
				result.Add(new EnemyCard(data, false));
				result.Add(new EnemyCard(data, true));
			}
			result.Sort((a, b) => a.cost.CompareTo(b.cost));
			return result;
		}

		private void RefreshCardWeights()
		{
			foreach (EnemyCard card in cards) card.RefreshWeightAndCost();
			cards.Sort((a, b) => a.cost.CompareTo(b.cost));
		}

		public void AddCredits()
		{
			if (!EnemyManager.Instance.HasMaxEnemies()) credits += GetCreditsPerSecond();
		}

		public float GetCreditsPerSecond()
		{
			return GetBaseCreditsPerSecond() * GetMultiplier();
		}

		public virtual List<Enemy> SpendCredits(bool useWeights = true)
		{
			spawnedEnemies.Clear();
			int spawnedThisCycle = 0;
			while (credits > 0f && spawnedThisCycle < maxEnemiesPerCycle && enemiesThisSecond < maxEnemiesPerSecond)
			{
				if (EnemyManager.Instance.HasMaxEnemies() && !ForceSpawn()) break;
				EnemyCard card = GetRandomCard(useWeights);
				if (card == null) break;
				credits -= card.cost;
				Enemy enemy = EnemyManager.Instance.SpawnEnemy(card.enemy, id, ForceSpawn(),
					card.isElite ? EEnemyFlag.Elite : EEnemyFlag.None, UseDirectionBias());
				spawnedEnemies.Add(enemy);
				enemiesThisSecond++;
				spawnedThisCycle++;
			}
			return spawnedEnemies;
		}

		protected EnemyCard GetRandomCard(bool useWeights)
		{
			var affordable = new List<EnemyCard>();
			foreach (EnemyCard card in isWaveMode ? waveModeCards : cards)
				if (credits >= card.cost) affordable.Add(card);
			if (affordable.Count == 0) return null;
			if (!useWeights)
			{
				EnemyCard mostExpensive = affordable[0];
				foreach (EnemyCard card in affordable) if (card.cost > mostExpensive.cost) mostExpensive = card;
				return mostExpensive;
			}
			float total = 0f;
			foreach (EnemyCard card in affordable) total += card.weight;
			float choice = (float)MyRandom.random.NextDouble() * total;
			float cumulative = 0f;
			foreach (EnemyCard card in affordable)
			{
				cumulative += card.weight;
				if (cumulative > choice) return card;
			}
			return null;
		}

		public void SetSlowmode(float multiplier, float duration)
		{
			slowmodeMultiplier = multiplier;
			slowmode = true;
			slowmodeOverAtTime = MyTime.time + duration;
		}

		public void SetWaveMode(List<EEnemy> waveEnemies, float duration)
		{
			isWaveMode = true;
			waveModeOverAtTime = MyTime.time + duration;
			waveModeCards = GenerateCards(waveEnemies);
		}

		protected float GetMultiplier()
		{
			return UseMultiplier() ? 1f + 0.7f * PlayerStats.GetStat(EStat.Difficulty) + 0.4f * MapController.GetStageIndex() : 1f;
		}

		protected virtual bool UseMultiplier()
		{
			return true;
		}

		private bool CanEarnCredits()
		{
			return false;
		}

		private void OnStatUpdated(EStat stat)
		{
			if (stat == EStat.EliteSpawnIncrease) RefreshCardWeights();
		}

		private void OnPlayerInventoryInitialized(PlayerInventory playerInventory)
		{
			RefreshCardWeights();
		}

		protected abstract void Init();

		protected abstract List<EEnemy> GetEnemies();

		public abstract float GetSummonInterval();

		public abstract float GetBaseCreditsPerSecond();

		public abstract float GetInitialCredits();

		public abstract int GetNumTargetEnemies();

		protected abstract bool UseDirectionBias();

		protected abstract bool ForceSpawn();
	}
}
