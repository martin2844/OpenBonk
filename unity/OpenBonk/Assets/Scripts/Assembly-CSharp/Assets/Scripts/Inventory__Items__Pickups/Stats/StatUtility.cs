using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.Mouse;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public static class StatUtility
	{
		public static string GetUpgradeDescriptionWeapon(List<StatModifier> modifiers, WeaponData weaponData)
		{
			var descriptions = new List<string>();
			foreach (var modifier in modifiers) descriptions.Add(GetUpgradeDescriptionWeaponModifier(modifier, weaponData));
			return string.Join("\n", descriptions);
		}

		public static string GetUpgradeDescriptionTome(List<StatModifier> modifiers, TomeData tomeData)
		{
			var descriptions = new List<string>();
			foreach (var modifier in modifiers) descriptions.Add(GetUpgradeDescriptionTomeModifier(modifier, tomeData));
			return string.Join("\n", descriptions);
		}

		public static string GetUpgradeDescriptionWeaponModifier(StatModifier modifier, WeaponData weaponData, string color = "#ffffff")
		{
			var weapon = MyPlayer.Instance.inventory.weaponInventory.weapons[weaponData.eWeapon];
			float current = weapon.GetValue(modifier.stat);
			float next = weapon.GetTestUpdateStat(modifier.stat, modifier);
			return Tooltip.GetTooltipString(modifier.stat) + ": " +
				GetWeaponModificationString(modifier.modifyType, modifier.stat, current, false) +
				" <sprite name=arrow> <color=green>" +
				GetWeaponModificationString(modifier.modifyType, modifier.stat, next, false) + "</color>";
		}

		public static string GetUpgradeDescriptionTomeModifier(StatModifier modifier, TomeData tomeData, string color = "#ffffff")
		{
			float current = 0f;
			float next = modifier.modification;
			bool addOneToMultiplication = true;
			if (tomeData != null && MyPlayer.Instance.inventory.tomeInventory.HasTome(tomeData.eTome))
			{
				current = MyPlayer.Instance.inventory.tomeInventory.tomeUpgrade[tomeData.eTome].modification;
				next += current;
				addOneToMultiplication = false;
			}
			return Tooltip.GetTooltipString(modifier.stat) + ": " +
				GetModificationString(modifier.modifyType, modifier.stat, current, addOneToMultiplication, false) +
				" <sprite name=arrow> <color=green>" +
				GetModificationString(modifier.modifyType, modifier.stat, next, addOneToMultiplication, false) + "</color>";
		}

		public static string GetUpgradeDescriptionStat(StatModifier modifier, string color = "#ffffff")
		{
			string upgrade = EncapsulateNumber(GetModificationString(modifier), color) + " " + Tooltip.GetTooltipString(modifier.stat);
			return Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Game_Ui", "SHRINE_CHARGE_UPGRADE", new Dictionary<string, string> { { "upgrade", upgrade } });
		}

		public static string EncapsulateNumber(string number, string color)
		{
			return "<b><color=" + color + ">" + number + "</color></b>";
		}

		public static string EncapsulateNumber(float number, string color)
		{
			return string.Format("<b><color={0}>{1}</color></b>", color, number);
		}

		public static string GetModificationString(StatModifier modifier, bool addOneToMultiplication = true)
		{
			return GetModificationString(modifier.modifyType, modifier.stat, modifier.modification, addOneToMultiplication);
		}

		public static string GetModificationString(EStatModifyType modifyType, EStat stat, float value, bool addOneToMultiplication = true, bool usePrefix = true)
		{
			string prefix = "", suffix = "";
			if (modifyType == EStatModifyType.Addition ||
				(modifyType == EStatModifyType.Flat && (stat == EStat.Armor || stat == EStat.Evasion || stat == EStat.Lifesteal || stat == EStat.CritChance || stat == EStat.CritDamage || stat == EStat.FallDamageReduction || stat == EStat.Luck || stat == EStat.Difficulty)))
			{
				value *= 100f;
				suffix = "%";
			}
			else if (modifyType == EStatModifyType.Multiplication)
			{
				if (addOneToMultiplication) value += 1f;
				suffix = "x";
			}
			else if (modifyType == EStatModifyType.Flat && value >= 0f) prefix = "+";
			value = (float)System.Math.Round(value, 2);
			if (!usePrefix) prefix = "";
			if (suffix == "x") return prefix + MyStringUtil.ShowOnlyDecimals(value) + suffix;
			return System.Math.Round(value, 1) % 1 == 0
				? string.Format("{0}{1:N0}{2}", prefix, value, suffix)
				: string.Format("{0}{1:N1}{2}", prefix, value, suffix);
		}

		public static string GetWeaponModificationString(EStatModifyType modifyType, EStat stat, float value, bool addOneToMultiplication = true)
		{
			if (stat == EStat.ProjectileSpeedMultiplier || stat == EStat.KnockbackMultiplier) return string.Format("{0:N1}", value);
			if (stat == EStat.DurationMultiplier) return MyStringUtil.ShowOnlyDecimals((float)System.Math.Round(value, 2)) + "s";
			return GetModificationString(modifyType, stat, value, true, false);
		}

		private static string ModifyStatName(string statName, EWeapon eWeapon)
		{
			return statName;
		}

		public static float GetRarityValue(float value, ERarity rarity, int decimals = 2)
		{
			return (float)System.Math.Round(value * Rarity.GetMultiplier(rarity), decimals);
		}

		public static EStatCategory GetStatCategory(EStat eStat)
		{
			// The original dispatches through a 47-entry category table, with
			// unused slots and later enum values taking the Null fallback.
			int value = (int)eStat;
			if (value >= 0 && value <= 8) return EStatCategory.Defensive;
			if (value >= 9 && value <= 12) return EStatCategory.Offensive;
			if (value >= 15 && value <= 24) return EStatCategory.Offensive;
			if (value >= 25 && value <= 28) return EStatCategory.Movement;
			if (value >= 29 && value <= 41) return EStatCategory.Utility;
			if (value == 42 || value == 43 || value == 45) return EStatCategory.Offensive;
			if (value == 46) return EStatCategory.Movement;
			return EStatCategory.Null;
		}
	}
}
