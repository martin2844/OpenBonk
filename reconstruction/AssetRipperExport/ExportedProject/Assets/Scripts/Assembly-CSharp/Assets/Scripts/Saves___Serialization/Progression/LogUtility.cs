using Actors.Enemies;
using System;
using UnityEngine;

namespace Assets.Scripts.Saves___Serialization.Progression
{
	public static class LogUtility
	{
		public static int numMaxChallenges = 6;

		private static EnemyLog FindLog(EEnemy enemy)
		{
			var logs = SaveManager.Instance?.stats?.enemyLogs;
			return logs != null && logs.TryGetValue(enemy, out var entry) ? entry : null;
		}

		public static int GetNumMaxEntries()
		{
			return Enum.GetValues(typeof(EEnemy)).Length;
		}

		public static int GetNumUnlockedEntries()
		{
			int count = 0;
			foreach (EEnemy enemy in Enum.GetValues(typeof(EEnemy)))
				if (IsEntryUnlocked(enemy)) count++;
			return count;
		}

		public static bool IsEntryUnlocked(EEnemy enemy)
		{
			var entry = FindLog(enemy);
			return entry != null && entry.kills > 0;
		}

		public static void GetChallengeProgress(EEnemy eEnemy, out float currentChallengeProgress, out int numChallengesClaimed, out int numKills, out int numKillsForNextChallengeTier)
		{
			var entry = FindLog(eEnemy);
			numKills = entry != null ? entry.kills : 0;
			numChallengesClaimed = entry != null ? entry.currentChallengeTier : 0;
			numKillsForNextChallengeTier = GetNumChallengeKills(eEnemy, numChallengesClaimed);
			currentChallengeProgress = numKillsForNextChallengeTier > 0
				? Mathf.Clamp01((float)numKills / numKillsForNextChallengeTier) : 0f;
		}

		public static bool HasUnclaimedReward(EEnemy eEnemy)
		{
			GetChallengeProgress(eEnemy, out float progress, out int claimed, out _, out _);
			return progress >= 1f && claimed < numMaxChallenges;
		}

		public static bool HasAnyUnclaimedReward()
		{
			foreach (EEnemy enemy in Enum.GetValues(typeof(EEnemy)))
				if (HasUnclaimedReward(enemy)) return true;
			return false;
		}

		public static bool HasClaimedAllRewards(EEnemy eEnemy)
		{
			var entry = FindLog(eEnemy);
			return entry != null && entry.currentChallengeTier >= numMaxChallenges;
		}

		public static int GetNumChallengeKills(EEnemy eEnemy, int tier)
		{
			EnemyData enemy = DataManager.Instance != null ? DataManager.Instance.GetEnemyData(eEnemy) : null;
			if (enemy == null) return 0;
			float baseCount = tier > 0 ? (int)Mathf.Pow(10f, tier + 1) : 1f;
			return Mathf.CeilToInt(baseCount / enemy.creditCost);
		}

		public static int GetReward(EEnemy eEnemy)
		{
			var entry = FindLog(eEnemy);
			return entry != null ? (entry.currentChallengeTier + 1) * 100 : 0;
		}
	}
}
