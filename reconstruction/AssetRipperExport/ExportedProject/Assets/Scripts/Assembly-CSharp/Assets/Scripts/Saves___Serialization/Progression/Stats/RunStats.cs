using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	public static class RunStats
	{
		private static Dictionary<string, float> stats;

		public static Dictionary<string, DamageSource> damageSources;

		public static List<MyAchievement> achievements;

		public static Action<string, float> A_StatChange;

		public static void Init()
		{
			GameManager.A_RunStarted -= OnNewRun;
			Enemy.A_Damage -= OnEnemyDamaged;
			GameManager.A_RunStarted += OnNewRun;
			Enemy.A_Damage += OnEnemyDamaged;
			Reset();
		}

		public static void Cleanup()
		{
			GameManager.A_RunStarted -= OnNewRun;
			Enemy.A_Damage -= OnEnemyDamaged;
		}

		private static void OnNewRun()
		{
			Reset();
		}

		private static void Reset()
		{
			stats = new Dictionary<string, float>();
			damageSources = new Dictionary<string, DamageSource>(64);
			achievements = new List<MyAchievement>();
		}

		public static void AddValue(EMyStat stat, int value)
		{
			AddValue(TrackStats.GetStatString(stat), value);
		}

		public static int GetStat(EMyStat stat)
		{
			return GetStat(TrackStats.GetStatString(stat));
		}

		public static void AddValue(string stat, int value)
		{
			if (stats == null) Reset();
			stats.TryGetValue(stat, out float current);
			stats[stat] = current + value;
			A_StatChange?.Invoke(stat, stats[stat]);
		}

		public static int GetStat(string stat)
		{
			return stats != null && stats.TryGetValue(stat, out float value) ? (int)value : 0;
		}

		public static void AddAchievement(MyAchievement achievement)
		{
			if (achievements == null) Reset();
			achievements.Add(achievement);
		}

		private static void OnEnemyDamaged(Enemy enemy, DamageContainer dc)
		{
			if (dc == null || string.IsNullOrEmpty(dc.damageSource)) return;
			if (damageSources == null) Reset();
			if (!damageSources.TryGetValue(dc.damageSource, out DamageSource source))
				damageSources.Add(dc.damageSource, source = new DamageSource(dc.damageSource, MyTime.time));
			source.AddDamage(dc.damage);
		}
	}
}
