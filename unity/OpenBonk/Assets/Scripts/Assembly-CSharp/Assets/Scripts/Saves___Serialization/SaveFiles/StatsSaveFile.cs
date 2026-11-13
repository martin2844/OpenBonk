using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Stats;

namespace Assets.Scripts.Saves___Serialization.SaveFiles
{
	[Serializable]
	public class StatsSaveFile
	{
		public Dictionary<string, MyStat> stats = new Dictionary<string, MyStat>();

		public Dictionary<EEnemy, EnemyLog> enemyLogs = new Dictionary<EEnemy, EnemyLog>();

		public Dictionary<ESpeedrunTime, float> times = new Dictionary<ESpeedrunTime, float>();

		public void Init()
		{
			foreach (EMyStat stat in Enum.GetValues(typeof(EMyStat)))
			{
				string name = stat.ToString();
				if (!stats.ContainsKey(name)) stats.Add(name, new MyStat(name, 0f));
			}
			foreach (ESpeedrunTime type in Enum.GetValues(typeof(ESpeedrunTime)))
				if (!times.ContainsKey(type)) times.Add(type, 0f);
		}
	}
}
