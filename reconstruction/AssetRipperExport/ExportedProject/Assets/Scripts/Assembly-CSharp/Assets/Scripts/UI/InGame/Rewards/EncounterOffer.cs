using System;
using Assets.Scripts.Inventory__Items__Pickups;

namespace Assets.Scripts.UI.InGame.Rewards
{
	[Serializable]
	public class EncounterOffer
	{
		public ERarity rarity;

		public EffectStat[] effects;

		public string GetEffectsDescription()
		{
			string description = effects[0].effectType == Effects.EEncounterEffect.StatChangeBalanceShrine
				? Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Game_Ui", "BALANCE_SHRINE_TEXT") + "\n\n" : "";
			for (int i = 0; i < effects.Length; i++)
			{
				if (i > 0) description += "\n";
				description += effects[i].GetDescription();
			}
			return description;
		}

		public void ApplyEffects(bool showInScoreUi = true)
		{
			foreach (EffectStat effect in effects)
			{
				effect.ApplyEffect();
				string description = effect.effectType == Effects.EEncounterEffect.StatChange ? EnumUtility.EnumToReadable(effect.statModifier.stat) ?? "" : "";
				string number = effect.GetEffectNumber();
				if (showInScoreUi && (number != "" || description != "")) UiManager.Instance.scoreUi.AddScore(description, number, true, true, 1f);
			}
		}

		public bool CanAccept(out string reason)
		{
			reason = "";
			foreach (var effect in effects)
				if (!effect.CanApplyEffect(out string effectReason)) { reason = effectReason; return false; }
			return true;
		}
	}
}
