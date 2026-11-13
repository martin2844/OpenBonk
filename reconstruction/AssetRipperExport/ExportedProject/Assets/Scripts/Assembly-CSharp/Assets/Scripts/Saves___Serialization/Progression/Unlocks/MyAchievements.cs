using System;
using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts._Data.Progression;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Saves___Serialization.Progression.Unlocks
{
	public static class MyAchievements
	{
		public static bool testUnlockEverything;

		private static bool hasUnsavedChanges;

		public static Action<MyAchievement> A_Unlocked;

		public static Action<string> A_TryUnlock;

		private static Dictionary<string, List<MyAchievement>> statTrackers = new Dictionary<string, List<MyAchievement>>();

		private static bool startedTracking;

		public static int fakeCharacters;

		public static int fakeWeapons;

		public static int fakeItems;

		public static int fakeMaps;

		public static int fakeTomes;

		public static int fakeAchievements;

		private static HashSet<MyStat> queuedStatNames = new HashSet<MyStat>();

		private static float statTrackersCooldown = 1f;

		private static float nextStatTrackersCheck;

		private static float nextSaveTimeReady;

		private static float saveCooldown = 5f;

		public static bool IsTestUnlockEverything()
		{
			return false;
		}

		public static void Init()
		{
			TryStartStatTracking();
			MyStats.A_StatUpdated += OnStatUpdated;
			SaveManager.A_SavesLoaded += OnProgressionLoaded;
			DataManager.A_DataLoaded += OnDataLoaded;
			MyTime.A_Pause += OnPause;
			SaveManager.A_ProgressionSaved += OnProgressionSaved;
		}

		public static void Cleanup()
		{
			MyStats.A_StatUpdated -= OnStatUpdated;
			SaveManager.A_SavesLoaded -= OnProgressionLoaded;
			DataManager.A_DataLoaded -= OnDataLoaded;
			MyTime.A_Pause -= OnPause;
			SaveManager.A_ProgressionSaved -= OnProgressionSaved;
		}

		private static void TryStartStatTracking()
		{
			if (DataManager.Instance == null || !SaveManager.loaded || startedTracking) return;
			startedTracking = true;
			foreach (MyAchievement achievement in DataManager.Instance.unsortedAchievements)
			{
				if (!IsUnlocked(achievement) && !string.IsNullOrEmpty(achievement.statName) && MyStats.GetStat(achievement.statName) >= achievement.targetValue)
					TryUnlock(achievement.internalName);
				if (string.IsNullOrEmpty(achievement.statName) || SaveManager.Instance.progression.achievements.Contains(achievement.internalName)) continue;
				if (!statTrackers.TryGetValue(achievement.statName, out List<MyAchievement> tracked)) statTrackers.Add(achievement.statName, tracked = new List<MyAchievement>());
				tracked.Add(achievement);
			}
		}

		public static bool TryUnlock(string unlockName)
		{
			if (SaveManager.Instance.progression.achievements.Contains(unlockName)) return false;
			MyAchievement achievement = DataManager.Instance.GetAchievement(unlockName);
			if (achievement == null) return false;
			SaveManager.Instance.progression.CompleteAchievement(achievement);
			hasUnsavedChanges = true;
			A_TryUnlock?.Invoke(unlockName);
			A_Unlocked?.Invoke(achievement);
			return true;
		}

		private static bool AreAchievementsDisabled()
		{
			return false;
		}

		private static void OnStatUpdated(string statName, MyStat stat)
		{
			if (statTrackers.ContainsKey(statName)) queuedStatNames.Add(stat);
		}

		public static void Update()
		{
			if (queuedStatNames.Count == 0 || Time.time <= nextStatTrackersCheck) return;
			nextStatTrackersCheck = Time.time + statTrackersCooldown;
			var queued = new List<MyStat>(queuedStatNames);
			queuedStatNames.Clear();
			foreach (MyStat stat in queued)
			{
				if (!statTrackers.TryGetValue(stat.name, out List<MyAchievement> tracked)) continue;
				var completed = new List<MyAchievement>();
				foreach (MyAchievement achievement in tracked)
					if (stat.value >= achievement.targetValue) { TryUnlock(achievement.internalName); completed.Add(achievement); }
				foreach (MyAchievement achievement in completed) tracked.Remove(achievement);
				if (tracked.Count == 0) statTrackers.Remove(stat.name);
			}
		}

		public static bool IsAchievementDone(string achName)
		{
			return SaveManager.Instance.progression.achievements.Contains(achName);
		}

		public static bool CheckAchievementValue(string achievementName, int value)
		{
			MyAchievement achievement = DataManager.Instance != null ? DataManager.Instance.GetAchievement(achievementName) : null;
			if (achievement == null)
			{
				MyLogger.LogError("Achievement is null... ach: " + achievementName);
				return false;
			}
			if (achievement.targetValue == 0 && achievement.targetValueFloat == 0f && string.IsNullOrEmpty(achievement.targetValueString))
			{
				MyLogger.LogError("Achievement isn't tracking value... ach: " + achievementName);
				return false;
			}
			return value >= achievement.targetValue;
		}

		public static int GetAchievementTargetValue(string achName)
		{
			MyAchievement achievement = DataManager.Instance != null ? DataManager.Instance.GetAchievement(achName) : null;
			return achievement != null ? achievement.targetValue : 0;
		}

		public static float GetAchievementTargetValueFloat(string achName)
		{
			MyAchievement achievement = DataManager.Instance != null ? DataManager.Instance.GetAchievement(achName) : null;
			return achievement != null ? achievement.targetValueFloat : 0f;
		}

		public static bool IsUnlocked(UnlockableBase unlockable, out string requirementsString)
		{
			requirementsString = string.Empty;
			if (!unlockable.isEnabled)
			{
				requirementsString = "Available in the full release";
				return false;
			}
			MyAchievement achievement = unlockable.GetUnlockRequirement();
			UnlockableBase requirement = unlockable.GetUnlockableRequirement();
			bool unlocked = true;
			if (achievement != null)
			{
				bool completed = IsUnlocked(achievement);
				if (!completed) unlocked = false;
				string color = completed ? MyColorUtility.requirementCompletedColor : MyColorUtility.requirementMissingColor;
				requirementsString += "<color=" + color + ">* " + achievement.GetUnlockRequirement() + "\n";
			}
			if (requirement != null)
			{
				bool purchased = IsPurchased(requirement);
				if (!purchased) unlocked = false;
				string color = purchased ? MyColorUtility.requirementCompletedColor : MyColorUtility.requirementMissingColor;
				requirementsString += "<color=" + color + ">* Purchase " + requirement.GetUnlockableTypeDisplayString() + " - " + requirement.GetName() + "\n";
			}
			return unlocked;
		}

		public static bool IsUnlocked(MyAchievement myAchievement)
		{
			return myAchievement == null || !myAchievement.isEnabled || IsUnlocked(myAchievement.internalName);
		}

		public static bool IsUnlockedInternalNameAch(string achName)
		{
			return IsUnlocked(achName);
		}

		private static bool IsUnlocked(string unlockName)
		{
			return SaveManager.Instance.progression.achievements.Contains(unlockName);
		}

		public static bool IsPurchased(UnlockableBase unlockable)
		{
			return unlockable != null && (unlockable.GetPrice() <= 0 || SaveManager.Instance.progression.purchases.Contains(unlockable.GetInternalName()));
		}

		public static bool IsAvailable(UnlockableBase unlockable)
		{
			return IsPurchased(unlockable) && IsActivated(unlockable) && IsUnlocked(unlockable, out _);
		}

		public static bool IsActivated(UnlockableBase unlockable)
		{
			if (unlockable == null || !unlockable.isEnabled) return false;
			if ((unlockable.GetPrice() > 0 && unlockable.GetUnlockRequirement() != null) || unlockable.canAlwaysToggle)
				return !SaveManager.Instance.progression.inactivated.Contains(unlockable.GetInternalName());
			return true;
		}

		public static bool CanToggleActivation(UnlockableBase unlockable)
		{
			if (unlockable == null || SaveManager.Instance?.progression?.shopItems == null ||
				!SaveManager.Instance.progression.shopItems.TryGetValue(Assets.Scripts._Data.ShopItems.EShopItem.Toggler, out int level) || level <= 0) return false;
			if (unlockable.canAlwaysToggle) return true;
			return unlockable.GetPrice() > 0 && unlockable.GetUnlockRequirement() != null && !(unlockable is CharacterData) && !(unlockable is HatData);
		}

		private static void OnProgressionSaved()
		{
			hasUnsavedChanges = false;
		}

		private static void OnProgressionLoaded()
		{
			TryStartStatTracking();
		}

		private static void OnDataLoaded()
		{
			TryStartStatTracking();
		}

		private static void OnPause(bool paused)
		{
			if (Time.time < nextSaveTimeReady) { hasUnsavedChanges = true; return; }
			if (!paused || !hasUnsavedChanges) return;
			nextSaveTimeReady = Time.time + saveCooldown;
			SaveManager.Instance.SaveProgression();
		}

		public static int NumCompletedAchievements()
		{
			return SaveManager.Instance.progression.achievements.Count;
		}

		public static int NumTotalAchievements()
		{
			int total = 0;
			foreach (MyAchievement achievement in DataManager.Instance.unsortedAchievements)
				if (achievement.isEnabled && !achievement.IsHiddenInMenus()) total++;
			return total;
		}

		public static bool AreAllQuestsCompleted()
		{
			return NumCompletedAchievements() >= NumTotalAchievements();
		}

		public static void SyncToSteamAchievements()
		{
		}

		public static void GetAchievementTypeProgress(EAchievementType achievementType, out int completed, out int total, out int unclaimed)
		{
			completed = total = unclaimed = 0;
			foreach (MyAchievement achievement in DataManager.Instance.unsortedAchievements)
			{
				if (achievement.achievementType != achievementType || !achievement.isEnabled || !achievement.IsVisible()) continue;
				total++;
				if (IsUnlocked(achievement))
				{
					completed++;
					if (!achievement.IsClaimed()) unclaimed++;
				}
			}
		}
	}
}
