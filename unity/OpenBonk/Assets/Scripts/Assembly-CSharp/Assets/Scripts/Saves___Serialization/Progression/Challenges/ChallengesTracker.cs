using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.ShopItems;
using UnityEngine;

namespace Assets.Scripts.Saves___Serialization.Progression.Challenges
{
	public class ChallengesTracker
	{
		public const string MODIFIER_NO_MOVEMENT = "no_movement";

		public const string MODIFIER_NO_ITEMS = "no_items";

		public const string MODIFIER_NO_WEAPONS = "no_weapons";

		public const string MODIFIER_INVERTED_CONTROLS = "inverted_controls";

		public const string MODIFIER_BLIND = "blind";

		public const string MODIFIER_SPEEDRUN = "speedrun";

		public const string MODIFIER_CRYPT = "crypt";

		public const string MODIFIER_MINIMALIST = "minimalist";

		public const string MODIFIER_NO_XP = "no_xp";

		private static ChallengeWinCondition winCondition;

		public static ChallengeModifier[] challengeModifiers;

		private static HashSet<string> modifierNames = new HashSet<string>();

		public const float silverAddPerChallenge = 0.01f;

		private static bool victory;

		// The Android scene transition can initialize the same inventory again while
		// tier 1 is still stage index zero. Its permanent modifiers must only be added once.
		private static PlayerInventory initializedInventory;

		public static Action<ChallengeData> A_ChallengeCompleted;

		public static void Init()
		{
			Cleanup();
			MapController.A_NewRunStarted += OnNewRunStarted;
			GameManager.A_GameOver += OnGameOver;
			MyPlayer.A_PlayerInventoryInitialized += OnInventoryInitialized;
			EnemyManager.A_StageBossDied += OnStagebossDefeated;
		}

		public static void Cleanup()
		{
			MapController.A_NewRunStarted -= OnNewRunStarted;
			GameManager.A_GameOver -= OnGameOver;
			MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInitialized;
			EnemyManager.A_StageBossDied -= OnStagebossDefeated;
			CleanupChallengeModifiers();
			initializedInventory = null;
		}

		private static void OnNewRunStarted()
		{
			victory = false;
			initializedInventory = null;
			modifierNames.Clear();
			CleanupChallengeModifiers();
			ChallengeData challenge = GetCurrentChallenge();
			if (challenge == null) return;
			challengeModifiers = new ChallengeModifier[challenge.challengeModifiers.Length];
			for (int i = 0; i < challengeModifiers.Length; i++)
			{
				challengeModifiers[i] = UnityEngine.Object.Instantiate(challenge.challengeModifiers[i]);
				challengeModifiers[i].Init(challenge);
				modifierNames.Add(challengeModifiers[i].internalName);
			}
			if (challenge.winCondition == null) throw new InvalidOperationException("Challenge has no win condition: " + challenge.name);
			winCondition = UnityEngine.Object.Instantiate(challenge.winCondition);
			winCondition.Init(challenge);
		}

		public static ChallengeData GetCurrentChallenge()
		{
			return MapController.runConfig != null ? MapController.runConfig.challenge : null;
		}

		public static void Tick()
		{
			if (challengeModifiers == null) return;
			foreach (ChallengeModifier modifier in challengeModifiers) modifier.Tick();
		}

		private static void CleanupChallengeModifiers()
		{
			if (challengeModifiers != null)
			{
				foreach (ChallengeModifier modifier in challengeModifiers) modifier.Cleanup();
				challengeModifiers = null;
			}
			if (winCondition != null)
			{
				winCondition.Cleanup();
				winCondition = null;
			}
		}

		public static void CompleteChallenge()
		{
			ChallengeData challenge = GetCurrentChallenge();
			if (challenge == null) return;
			MyAchievements.TryUnlock(challenge.internalName);
			A_ChallengeCompleted?.Invoke(challenge);
		}

		public static bool HasChallengeModifier(string internalName)
		{
			return modifierNames.Contains(internalName);
		}

		public static List<string> GetChallengeModifiers()
		{
			return new List<string>(modifierNames);
		}

		private static void OnInventoryInitialized(PlayerInventory pinv)
		{
			if (!MapController.IsFirstStage() || pinv == null || ReferenceEquals(initializedInventory, pinv)) return;
			initializedInventory = pinv;
			if (SaveManager.Instance != null && SaveManager.Instance.stats != null)
			{
				int completedChallenges = (int)MyStats.GetStat(EMyStat.challengesCompleted.ToString());
				if (completedChallenges > 0)
					pinv.statInventory.ChangeStat(new StatModifier { stat = EStat.SilverIncreaseMultiplier, modifyType = EStatModifyType.Addition, modification = completedChallenges * 0.01f }, true, 0f, true);
			}
			float silverMultiplier = MapController.runConfig.GetSilverMultiplier();
			if (!Mathf.Approximately(silverMultiplier, 1f))
				pinv.statInventory.ChangeStat(new StatModifier { stat = EStat.SilverIncreaseMultiplier, modifyType = EStatModifyType.Multiplication, modification = silverMultiplier }, true, 0f, true);
			if (DataManager.Instance != null && SaveManager.Instance != null && SaveManager.Instance.progression != null)
			{
				ShopItemData silver = DataManager.Instance.GetShopItemData(EShopItem.Silver);
				if (silver != null && silver.GetLevel() > 0)
					pinv.statInventory.ChangeStat(new StatModifier { stat = EStat.SilverIncreaseMultiplier, modifyType = EStatModifyType.Addition, modification = silver.GetLevel() * silver.value / 100f }, true, 0f, true);
			}
			if (challengeModifiers == null) return;
			foreach (ChallengeModifier modifier in challengeModifiers)
			{
				foreach (var stat in modifier.statModifiers)
					MyPlayer.Instance.inventory.statInventory.ChangeStat(stat, true, 0f, false);
				modifier.OnStatsApplied();
			}
		}

		public static bool HasChallenge()
		{
			return GetCurrentChallenge() != null && challengeModifiers != null && challengeModifiers.Length > 0;
		}

		private static void OnGameOver()
		{
			if (MapController.currentMap == null || MapController.runConfig == null || MyPlayer.Instance == null || SaveManager.Instance == null || SaveManager.Instance.progression == null) return;
			SaveManager.Instance.progression.menuMeta.GetMapProgress(MapController.currentMap.eMap)
				.OnRunFinished(MyPlayer.Instance.character, victory, MapController.runConfig.mapTierIndex);
		}

		private static void OnStagebossDefeated()
		{
			if (!MapController.IsTierFinalStage()) return;
			victory = true;
			if (SaveManager.Instance != null && SaveManager.Instance.progression != null && MapController.currentMap != null)
				SaveManager.Instance.progression.menuMeta.GetMapProgress(MapController.currentMap.eMap).SetCompletedTime(MyTime.runTimer);
		}
	}
}
