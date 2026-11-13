using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using UnityEngine;

namespace Assets.Scripts.Managers
{
	public class EnemyManager : MonoBehaviour
	{
		public EnemyData testEnemy;

		public Dictionary<uint, Enemy> enemies = new Dictionary<uint, Enemy>();

		public Dictionary<Collider, Enemy> collidersToEnemies = new Dictionary<Collider, Enemy>();

		public Dictionary<GameObject, Enemy> gameobjectsToEnemies = new Dictionary<GameObject, Enemy>();

		private Dictionary<int, int> waveEnemies = new Dictionary<int, int>();

		private uint id;

		public static EnemyManager Instance;

		public bool enabledWaves;

		public SummonerController summonerController;

		private List<Enemy> stageBosses = new List<Enemy>();

		public static Action A_StageBossDied;

		public static int maxNumEnemiesPooled = 700;

		public int numEnemies;

		private float nextDebuffTickTime;

		private readonly List<Enemy> enemyUpdateBuffer = new List<Enemy>();

		private float zoomValue;

		private float currentValue;

		private bool started;

		public bool stageBossIsDead { get; private set; }

		private void Awake()
		{
			if (Instance != null) { Destroy(gameObject); return; }
			Instance = this;
			Enemy.A_EnemyReleasedFromPool += RemoveEnemy;
		}

		private void Start()
		{
			summonerController = new SummonerController();
		}

		private void OnDestroy()
		{
			if (Instance != this) return;
			Enemy.A_EnemyReleasedFromPool -= RemoveEnemy;
			// Start may not have run when a scene is torn down during startup.
			summonerController?.Cleanup();
		}

		public Enemy SpawnEnemy(EnemyData enemyData, int summonerId, bool forceSpawn, EEnemyFlag flag = EEnemyFlag.None, bool useDirectionBias = true)
		{
			Vector3 pos = SpawnPositions.GetEnemySpawnPosition(enemyData, 50, useDirectionBias);
			if (pos == SpawnPositions.INVALID_POS) return null;
			return SpawnEnemy(enemyData, pos, summonerId, forceSpawn, flag);
		}

		public Enemy SpawnEnemy(EnemyData enemyData, Vector3 pos, int waveNumber, bool forceSpawn = false, EEnemyFlag flag = EEnemyFlag.None, bool canBeElite = true, float extraSizeMultiplier = 1f)
		{
			if (pos == SpawnPositions.INVALID_POS || (!forceSpawn && !CanSpawnEnemy())) return null;
			GameObject obj = PoolManager.Instance.enemyPool.Get();
			if (obj == null) return null;
			if (!gameobjectsToEnemies.TryGetValue(obj, out Enemy enemy))
			{
				enemy = obj.GetComponent<Enemy>();
				gameobjectsToEnemies.Add(obj, enemy);
			}
			enemy.InitEnemy(id++, enemyData, pos, waveNumber, flag, canBeElite, extraSizeMultiplier);
			enemies.Add(enemy.id, enemy);
			if (!collidersToEnemies.ContainsKey(enemy.collider)) collidersToEnemies.Add(enemy.collider, enemy);
			if (!gameobjectsToEnemies.ContainsKey(enemy.gameObject)) gameobjectsToEnemies.Add(enemy.gameObject, enemy);
			if (!waveEnemies.ContainsKey(waveNumber)) waveEnemies.Add(waveNumber, 0);
			waveEnemies[waveNumber]++;
			numEnemies++;
			return enemy;
		}

		public Enemy SpawnBoss(EEnemy eEnemy, int summonerId, EEnemyFlag enemyFlag, Vector3 pos, float extraSizeMultiplier = 1f)
		{
			EnemyData data = DataManager.Instance.GetEnemyData(eEnemy);
			if (data == null) return null;
			Enemy enemy = SpawnEnemy(data, pos, summonerId, true, enemyFlag, false, extraSizeMultiplier);
			if (enemy == null) return null;
			if (eEnemy != EEnemy.GhostInvincible && GameManager.Instance.bossCurses > 0)
				enemy.SetSwarmMultiplierHp(1f + GameManager.Instance.bossCurses * 0.16f);
			if ((enemyFlag & EEnemyFlag.StageBoss) != 0) stageBosses.Add(enemy);
			return enemy;
		}

		public void RemoveEnemy(Enemy enemy)
		{
			enemies.Remove(enemy.id);
			numEnemies--;
			if (waveEnemies.ContainsKey(enemy.waveNumber)) waveEnemies[enemy.waveNumber]--;
			if (enemy.IsStageBoss())
			{
				stageBosses.Remove(enemy);
				if (stageBosses.Count <= 0) StageBossDied();
			}
		}

		private void StageBossDied()
		{
			stageBosses = new List<Enemy>();
			stageBossIsDead = true;
			A_StageBossDied?.Invoke();
		}

		public bool CanSpawnEnemy()
		{
			return numEnemies < GetNumMaxEnemies();
		}

		public bool HasMaxEnemies()
		{
			return numEnemies >= GetNumMaxEnemies();
		}

		public int GetNumMaxEnemies()
		{
			return IsFinalSwarm() ? (MyTime.finalSwarmTimer < 120f ? 400 : 300) : 550;
		}

		public int GetNumEnemies()
		{
			return numEnemies;
		}

		public int GetEnemiesFromSummoner(int wave)
		{
			return waveEnemies.TryGetValue(wave, out int count) ? count : 0;
		}

		public bool IsFinalSwarm()
		{
			return summonerController != null && summonerController.IsFinalSwarm();
		}

		private void Update()
		{
		}

		private void FixedUpdate()
		{
			if (GameManager.Instance.GetAliveTime() <= 0f || MyTime.paused) return;
			if (MyTime.time >= nextDebuffTickTime)
			{
				nextDebuffTickTime = MyTime.time + DebuffUtility.debuffCooldownSeconds;
				enemyUpdateBuffer.Clear();
				enemyUpdateBuffer.AddRange(enemies.Values);
				foreach (Enemy enemy in enemyUpdateBuffer) enemy.DebuffTick();
			}
			enemyUpdateBuffer.Clear();
			enemyUpdateBuffer.AddRange(enemies.Values);
			foreach (Enemy enemy in enemyUpdateBuffer) enemy.MyFixedUpdate();
			if (enabledWaves) summonerController.Tick();
		}

		public bool GetEnemy(Collider collider, out Enemy enemy)
		{
			bool found = collidersToEnemies.TryGetValue(collider, out enemy);
			if (!found) MyLogger.LogError("AAH COLLIDER TO ENEMY FAILED? WTF?");
			return enemy != null ? found && !enemy.IsDeadOrDyingNextFrame() : found;
		}

		public bool GetEnemy(GameObject enemyObject, out Enemy enemy)
		{
			bool found = gameobjectsToEnemies.TryGetValue(enemyObject, out enemy);
			if (!found) MyLogger.LogErrorInBuild("AAH COLLIDER TO ENEMY FAILED? WTF?");
			return found;
		}

		private void TestSpawnEnemy()
		{
		}
	}
}
