using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Me/Progression/Challenge", order = 1)]
public class ChallengeData : MyAchievement
{
	[Header("Challenge")]
	public EMap map;

	public int tier;

	public float silverMultiplier;

	public string suggestionAuthor;

	public int requiresNumChallengesCompleted;

	public ChallengeData prerequisiteChallenge;

	public ChallengeModifier[] challengeModifiers;

	public ChallengeWinCondition winCondition;

	public override string GetDisplayName()
	{
		if (localizedName == null || localizedName.IsEmpty) return unlockable != null ? unlockable.GetName() : string.Empty;
		string value = localizedName.GetLocalizedString();
		return tier > 0 ? value + " " + (tier + 1) : value;
	}

	public override string GetUnlockRequirement()
	{
		var keys = new Dictionary<string, string> { ["tier"] = (tier + 1).ToString() };
		string tierName = LocalizationUtility.GetLocalizedString("Other", "TIER_SMART", keys);
		MapData mapData = DataManager.Instance != null ? DataManager.Instance.GetMap(map) : null;
		return "[" + (mapData != null ? mapData.GetName() : map.ToString()) + " " + tierName + " - " + GetDisplayName();
	}

	public bool CanShow()
	{
		if (prerequisiteChallenge != null && (SaveManager.Instance == null || SaveManager.Instance.progression == null || !MyAchievements.IsUnlocked(prerequisiteChallenge))) return false;
		if (requiresNumChallengesCompleted <= 0) return true;
		return SaveManager.Instance != null && SaveManager.Instance.stats != null &&
			MyStats.GetStat(EMyStat.challengesCompleted.ToString()) >= requiresNumChallengesCompleted;
	}

	public string GetSilverMultiplier()
	{
		return MyStringUtil.ShowOnlyDecimals(silverMultiplier);
	}

	public override int CompareTo(MyAchievement otherAch)
	{
		if (otherAch == null) return 1;
		if (!(otherAch is ChallengeData other)) return base.CompareTo(otherAch);
		int result = map.CompareTo(other.map);
		if (result != 0) return result;
		result = tier.CompareTo(other.tier);
		if (result != 0) return result;
		result = sortingOrder.CompareTo(other.sortingOrder);
		return result != 0 ? result : string.Compare(internalName, other.internalName, System.StringComparison.Ordinal);
	}
}
