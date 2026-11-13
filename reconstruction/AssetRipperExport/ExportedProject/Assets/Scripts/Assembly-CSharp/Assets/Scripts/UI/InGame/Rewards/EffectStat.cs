using System;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.UI.InGame.Rewards.Effects;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using UnityEngine;
using Assets.Scripts.UI.Mouse;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using System.Collections.Generic;

namespace Assets.Scripts.UI.InGame.Rewards
{
	[Serializable]
	public class EffectStat
	{
		public EEncounterEffect effectType;

		public StatModifier statModifier;

		public bool permanent = true;

		public float duration;

		public float value;

		public bool isPositiveEffect = true;

		public string GetDescription()
		{
			string color = GetColor();
			switch (effectType)
			{
				case EEncounterEffect.StatChange: return GetStatDescription(color);
				case EEncounterEffect.EGold:
					if (value < 0f) return "Lose " + StatUtility.EncapsulateNumber(value, color) + " gold";
					return LocalizationUtility.GetLocalizedString("Game_Ui", "SHRINE_GREEDY_GOLD", new Dictionary<string, string> { { "gold_icon", "<size=110%><sprite name=gold></size>" }, { "gold_amount", GetValue().ToString() } });
				case EEncounterEffect.EHealth:
					return (value < 0f ? "Lose " : "Heal ") + StatUtility.EncapsulateNumber((value * 100f).ToString() + "%", color) + " HP of max HP";
				case EEncounterEffect.StatChangeBalanceShrine:
					return statModifier.modification < 1f ? "Halve " + Tooltip.GetTooltipString(statModifier.stat) + " <color=red>(-0.5x)</color>" : "Double " + Tooltip.GetTooltipString(statModifier.stat) + " <color=green>(+2.0x)</color>";
				default: return "No description defined u oh";
			}
		}

		private float GetValue()
		{
			return effectType == EEncounterEffect.EGold ? MoneyUtility.GetChestPrice() * value : value;
		}

		private string GetStatDescription(string color)
		{
			string description = StatUtility.GetUpgradeDescriptionStat(statModifier, color);
			return permanent ? description ?? "" : string.Format("{0} for {1} seconds", description, duration);
		}

		public string GetShortDescription()
		{
			if (effectType == EEncounterEffect.StatChange) return StatUtility.EncapsulateNumber(StatUtility.GetModificationString(statModifier), GetColor()) + " " + EnumUtility.EnumToReadable(statModifier.stat);
			if (effectType == EEncounterEffect.EGold || effectType == EEncounterEffect.EHealth) return "";
			return "No description defined u oh";
		}

		public string GetEffectNumber()
		{
			return effectType == EEncounterEffect.StatChange
				? StatUtility.EncapsulateNumber(StatUtility.GetModificationString(statModifier), Assets.Scripts.Utility.MyColorUtility.GetStatTextColor(isPositiveEffect))
				: "";
		}

		public string GetEffectName()
		{
			return effectType == EEncounterEffect.StatChange ? EnumUtility.EnumToReadable(statModifier.stat) ?? "" : "";
		}

		public void ApplyEffect()
		{
			switch (effectType)
			{
				case EEncounterEffect.StatChange:
				case EEncounterEffect.StatChangeBalanceShrine:
					MyPlayer.Instance.inventory.statInventory.ChangeStat(statModifier, permanent, duration, true); break;
				case EEncounterEffect.EGold:
					MyPlayer.Instance.inventory.ChangeGold((int)GetValue()); break;
				case EEncounterEffect.EHealth:
					HealthEffect(); break;
			}
		}

		private string GetColor()
		{
			return MyColorUtility.GetStatTextColor(isPositiveEffect);
		}

		private void HealthEffect()
		{
			var health = MyPlayer.Instance.inventory.playerHealth;
			float amount = Mathf.Abs(value) * health.maxHp;
			if (value > 0f) health.Heal((int)amount, true);
			else health.DamagePlayerExternal(amount, 0f, Vector3.zero, false, "Enemy");
		}

		public bool CanApplyEffect(out string reason)
		{
			reason = "";
			if (effectType == EEncounterEffect.EGold && value < 0f)
			{
				reason = "Not enough gold";
				return MyPlayer.Instance.inventory.gold >= -value;
			}
			if (effectType == EEncounterEffect.EHealth && value < 0f)
			{
				reason = "Not enough HP";
				var health = MyPlayer.Instance.inventory.playerHealth;
				return health.hp > Mathf.Abs(value) * health.maxHp;
			}
			return true;
		}
	}
}
