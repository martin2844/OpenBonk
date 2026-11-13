using System;
using System.Collections.Generic;
using Actors.Enemies;

namespace Assets.Scripts.Game.Spawning
{
	[Serializable]
	public class EnemyWave
	{
		public EWaveType eWaveType;

		public List<EEnemy> enemies;

		public List<EnemyData> enemyDatas;

		public int waveTime = 60;

		public int minNumEnemies = 50;

		public float spawnInterval = 5f;

		public override string ToString()
		{
			return string.Format("{0} | waveTime: {1} | minNumEnemies: {2}", eWaveType, waveTime, minNumEnemies);
		}

		public float EnemiesPerSecond()
		{
			return minNumEnemies / spawnInterval;
		}
	}
}
