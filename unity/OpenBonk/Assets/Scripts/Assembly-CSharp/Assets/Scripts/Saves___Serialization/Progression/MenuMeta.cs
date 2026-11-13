using System;
using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Saves___Serialization.Progression.Stats;

namespace Assets.Scripts.Saves___Serialization.Progression
{
	[Serializable]
	public class MenuMeta
	{
		public EMap lastSelectedMap = EMap.Forest;

		public Dictionary<EMap, MapProgress> mapsProgress = new Dictionary<EMap, MapProgress>();

		private int numRunsForUnlocks = 1;

		private int numRunsForLeaderboards = 2;

		private int numRunsForQuests = 4;

		private int numRunsForQuickQuests = 5;

		private int numRunsForShop = 6;

		public bool hasVisitedUnlocks;

		public bool hasVisitedQuests;

		public bool hasVisitedShop;

		public bool HasMenuUnlocks()
		{
			return HasRuns(numRunsForUnlocks);
		}

		public bool HasMenuQuests()
		{
			return HasRuns(numRunsForQuests);
		}

		public bool HasMenuShop()
		{
			return HasRuns(numRunsForShop);
		}

		public bool HasQuickQuests()
		{
			return HasRuns(numRunsForQuickQuests);
		}

		public bool HasLeaderboards()
		{
			return SaveManager.Instance?.config?.cfGameSettings != null && SaveManager.Instance.config.cfGameSettings.hide_leaderboards != 1 && HasRuns(numRunsForLeaderboards);
		}

		private static bool HasRuns(int count)
		{
			return SaveManager.Instance?.stats?.stats != null && MyStats.GetStat(EMyStat.runs.ToString()) >= count;
		}

		public MapProgress GetMapProgress(EMap map)
		{
			VerifyMap(map);
			return mapsProgress[map];
		}

		private void VerifyMap(EMap map)
		{
			mapsProgress ??= new Dictionary<EMap, MapProgress>();
			if (!mapsProgress.ContainsKey(map) || mapsProgress[map] == null) mapsProgress[map] = new MapProgress();
		}

		public void SetTier(EMap map, int tier)
		{
			GetMapProgress(map).lastSelectTier = tier;
		}

		public void SetTierCompletion(EMap map, int tier)
		{
			GetMapProgress(map).CompleteTier(tier);
		}

		public int GetLastSelectedTier(EMap map)
		{
			return GetMapProgress(map).lastSelectTier;
		}

		public bool IsTierCompleted(EMap map, int tier)
		{
			return GetMapProgress(map).IsTierCompleted(tier);
		}

		public int GetHighestCompletedTier(EMap map)
		{
			int highest = -1;
			foreach (int tier in GetMapProgress(map).completedTiers) if (tier > highest) highest = tier;
			return highest;
		}
	}
}
