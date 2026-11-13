using System;
using System.Collections.Generic;
using Assets.Scripts._Data.Progression;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Saves___Serialization.Progression.Achievements
{
	[CreateAssetMenu(menuName = "Me/Progression/Unlock", order = 1)]
	public class MyAchievement : ScriptableObject, IComparable<MyAchievement>
	{
		public LocalizedString localizedName;

		public LocalizedString localizedDescription;

		public bool isEnabled;

		public bool isHidden;

		[HideInInspector]
		public string internalName;

		public string statName;

		public int targetValue;

		public float targetValueFloat;

		public string targetValueString;

		public Texture icon;

		public int sortingOrder;

		public EAchievementDifficulty difficulty;

		public EAchievementType achievementType;

		public List<LocalizationKey> serializedLocalizationKeys;

		public int achIteration;

		public bool useIterations;

		public UnlockableBase unlockable { get; private set; }

		public string GetUnlockDescription()
		{
			if (localizedDescription.IsEmpty) return string.Empty;
			Dictionary<string, string> keys = GetKeys();
			return keys.Count == 0 ? localizedDescription.GetLocalizedString() : localizedDescription.GetLocalizedString(keys);
		}

		public virtual string GetDisplayName()
		{
			if (localizedName == null || localizedName.IsEmpty) return unlockable != null ? unlockable.GetName() : string.Empty;
			string result = localizedName.GetLocalizedString();
			return useIterations ? result + " " + (achIteration + 1) : result;
		}

		private Dictionary<string, string> GetKeys()
		{
			var keys = new Dictionary<string, string>();
			if (IsUsingTargetValue())
				keys.Add("value", targetValueFloat != 0f ? targetValueFloat.ToString() : !string.IsNullOrEmpty(targetValueString) ? targetValueString : targetValue.ToString("N0"));
			if (serializedLocalizationKeys != null)
				foreach (var key in serializedLocalizationKeys) keys.Add(key.key, key.localizedValue.GetLocalizedString());
			return keys;
		}

		public Texture GetIcon()
		{
			return unlockable != null ? unlockable.GetIcon() : icon;
		}

		public bool IsUsingTargetValue()
		{
			return targetValue != 0 || targetValueFloat != 0f || !string.IsNullOrEmpty(targetValueString);
		}

		public bool IsTrackingStat()
		{
			return !string.IsNullOrEmpty(statName);
		}

		public virtual string GetUnlockRequirement()
		{
			return GetUnlockDescription();
		}

		public string GetUnlockedString()
		{
			return unlockable != null ? "<sprite name=unlock> " + unlockable.GetUnlockableTypeDisplayString() + " - " + unlockable.GetName() : string.Empty;
		}

		public string GetRewardString()
		{
			var silver = Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedStringReference("Other", "SILVER");
			return string.Format("<size=110%><sprite name=silver></size> {0} {1}", GetSilverReward(), silver.GetLocalizedString());
		}

		public void SetUnlockable(UnlockableBase unlockable)
		{
			this.unlockable = unlockable;
		}

		public bool IsCompleted()
		{
			return Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(this);
		}

		public bool IsClaimed()
		{
			return SaveManager.Instance.progression.claimedAchievements.Contains(internalName);
		}

		public float GetProgress()
		{
			return IsTrackingStat() ? Mathf.Clamp01(Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat(statName) / targetValue) : (IsUnlocked() ? 1f : 0f);
		}

		public int GetCurrentValue()
		{
			return IsTrackingStat() ? Mathf.RoundToInt(Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat(statName)) : (IsUnlocked() ? 1 : 0);
		}

		public bool IsHiddenInMenus()
		{
			return isHidden && !IsUnlocked();
		}

		private void OnValidate()
		{
		}

		public bool IsVisible()
		{
			if (!isEnabled || IsHiddenInMenus()) return false;
			if (achievementType == EAchievementType.Skins)
			{
				var skin = (SkinData)unlockable;
				if (!Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(DataManager.Instance.GetCharacterData(skin.character), out _)) return false;
			}
			if (achievementType == EAchievementType.Challenges)
			{
				var challenge = (ChallengeData)this;
				if (!Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(DataManager.Instance.GetMap(challenge.map), out _)) return false;
				if (!SaveManager.Instance.progression.menuMeta.IsTierCompleted(challenge.map, challenge.tier)) return false;
			}
			return true;
		}

		public bool IsUnlocked()
		{
			return Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(this);
		}

		public int GetSilverReward()
		{
			switch (difficulty)
			{
				case EAchievementDifficulty.Medium: return 2;
				case EAchievementDifficulty.Hard: return 4;
				case EAchievementDifficulty.Cooked: return 8;
				default: return 1;
			}
		}

		public virtual int CompareTo(MyAchievement other)
		{
			if (ReferenceEquals(this, other)) return 0;
			if (other == null) return 1;
			int order = sortingOrder.CompareTo(other.sortingOrder);
			if (order != 0) return order;
			if (unlockable != null && other.unlockable != null)
			{
				int unlockOrder = unlockable.CompareTo(other.unlockable);
				if (unlockOrder != 0) return unlockOrder;
			}
			int nameOrder = string.Compare(internalName, other.internalName, StringComparison.Ordinal);
			if (nameOrder != 0) return nameOrder;
			return GetSilverReward().CompareTo(other.GetSilverReward());
		}
	}
}
