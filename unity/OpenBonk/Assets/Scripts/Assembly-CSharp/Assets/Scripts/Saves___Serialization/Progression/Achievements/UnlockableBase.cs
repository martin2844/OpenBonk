using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Saves___Serialization.Progression.Achievements
{
	public abstract class UnlockableBase : ScriptableObject, IComparable<UnlockableBase>
	{
		public bool isEnabled;

		public bool showInUnlocks;

		public bool canAlwaysToggle;

		public string author;

		public int price;

		public int sortingPriority;

		public LocalizedString localizedName;

		public LocalizedString localizedDescription;

		public List<LocalizationKey> serializedLocalizationKeys;

		public List<LocalizationKey> serializedLocalizationKeysName;

		public virtual string GetName()
		{
			if (localizedName == null || localizedName.IsEmpty) return string.Empty;
			var keys = GetKeysName();
			return keys.Count > 0 ? localizedName.GetLocalizedString(keys) : localizedName.GetLocalizedString();
		}

		public virtual string GetDescription()
		{
			if (localizedDescription == null || localizedDescription.IsEmpty) return string.Empty;
			var keys = GetKeysDesc();
			return keys.Count > 0 ? localizedDescription.GetLocalizedString(keys) : localizedDescription.GetLocalizedString();
		}

		private Dictionary<string, string> GetKeysDesc()
		{
			var result = new Dictionary<string, string>();
			if (serializedLocalizationKeys != null)
				foreach (var entry in serializedLocalizationKeys) result.Add(entry.key, entry.localizedValue.GetLocalizedString());
			return result;
		}

		private Dictionary<string, string> GetKeysName()
		{
			var result = new Dictionary<string, string>();
			if (serializedLocalizationKeysName != null)
				foreach (var entry in serializedLocalizationKeysName) result.Add(entry.key, entry.localizedValue.GetLocalizedString());
			return result;
		}

		public virtual int GetPrice()
		{
			return Mathf.RoundToInt(price * 1.75f);
		}

		public abstract Texture GetIcon();

		public abstract MyAchievement GetUnlockRequirement();

		public abstract UnlockableBase GetUnlockableRequirement();

		public abstract string GetUnlockableTypeDisplayString();

		public abstract string GetInternalName();

		public bool CanBuy()
		{
			return SaveManager.Instance.progression.silver >= GetPrice();
		}

		public virtual int CompareTo(UnlockableBase other)
		{
			int priceComparison = GetPrice().CompareTo(other.GetPrice());
			return priceComparison != 0 ? priceComparison : sortingPriority.CompareTo(other.sortingPriority);
		}
	}
}
