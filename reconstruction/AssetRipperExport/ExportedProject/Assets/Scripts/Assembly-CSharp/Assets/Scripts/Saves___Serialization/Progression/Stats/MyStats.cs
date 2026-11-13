using System;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	public static class MyStats
	{
		private static bool hasUnsavedChanges;

		public static Action<string, MyStat> A_StatUpdated;

		private static float nextSaveTimeReady;

		private static float saveCooldown = 5f;

		public static void Init()
		{
			if (SaveManager.loaded) OnProgressionLoaded();
			SaveManager.A_SavesLoaded += OnProgressionLoaded;
			SaveManager.A_StatsSaved += OnProgressionSaved;
			MyTime.A_Pause += OnPause;
		}

		public static void Cleanup()
		{
			SaveManager.A_SavesLoaded -= OnProgressionLoaded;
			SaveManager.A_StatsSaved -= OnProgressionSaved;
			MyTime.A_Pause -= OnPause;
		}

		public static void AddValue(EMyStat statName, float value)
		{
			AddValue(TrackStats.GetStatString(statName), value);
		}

		public static void AddValue(string statName, float value)
		{
			var stats = SaveManager.Instance.stats.stats;
			if (!stats.TryGetValue(statName, out MyStat stat)) stats.Add(statName, stat = new MyStat(statName, 0f));
			SetValueInternal(statName, stat, stat.value + value);
		}

		private static void SetValueInternal(string statName, MyStat stat, float value)
		{
			if (stat.value > value) throw new Exception($"Cannot decrease statistic {statName} from {stat.value} to {value}");
			stat.value = value;
			hasUnsavedChanges = true;
			A_StatUpdated?.Invoke(statName, stat);
		}

		public static void SetValueForce(string statName, float value)
		{
			var stats = SaveManager.Instance.stats.stats;
			if (!stats.TryGetValue(statName, out MyStat stat)) stats.Add(statName, stat = new MyStat(statName, 0f));
			stat.value = value;
			hasUnsavedChanges = true;
			A_StatUpdated?.Invoke(statName, stat);
		}

		public static bool HasStat(string statName)
		{
			return SaveManager.Instance.stats.stats.ContainsKey(statName);
		}

		public static float GetStat(string statName)
		{
			return SaveManager.Instance.stats.stats.TryGetValue(statName, out MyStat stat) ? stat.value : 0f;
		}

		private static void OnProgressionLoaded()
		{
			hasUnsavedChanges = false;
		}

		private static void OnProgressionSaved()
		{
			hasUnsavedChanges = false;
		}

		private static void OnPause(bool paused)
		{
			if (Time.time < nextSaveTimeReady) { hasUnsavedChanges = true; return; }
			if (paused && hasUnsavedChanges)
			{
				nextSaveTimeReady = Time.time + saveCooldown;
				SaveManager.Instance.SaveStats();
			}
		}

		public static void SynToSteamStats()
		{
		}
	}
}
