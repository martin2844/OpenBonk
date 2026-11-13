using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Menu.Shop;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGiantFork : ItemBase
	{
		private float critChancePerAmount = .15f;

		private float megaCritChancePerAmount = .14f;

		private float megaCritChance;

		private float megaCritDamageMultiplier = 4f;

		private float extraDamageMultiplierPerAmount = .15f;

		private float finalMegacritMultiplier = 4f;

		protected override void OnInitOrAmountChanged()
		{
			megaCritChance = amount * megaCritChancePerAmount;
			SetStat(new StatModifier { stat = EStat.CritChance, modifyType = EStatModifyType.Flat, modification = amount * critChancePerAmount });
			finalMegacritMultiplier = megaCritDamageMultiplier + (amount >= 2 ? (amount - 1) * extraDamageMultiplierPerAmount : 0f);
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
			if (dc.crit && dc.enemy != null && ItemUtility.TryProc(megaCritChance, dc.procCoefficient))
			{
				dc.damageEffect = EDamageEffect.Megacrit;
				itemAttackModifier.AddMultiplier(finalMegacritMultiplier);
			}
		}

		public override bool HasPreAttackProc()
		{
			return true;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public ItemGiantFork(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string> {
				["value1"] = string.Format("{0}", megaCritChancePerAmount * 100f),
				["stat2"] = EnumUtility.EnumToReadable(EStat.CritChance) ?? "",
				["value2"] = string.Format("+{0}%", critChancePerAmount * 100f),
				["value3"] = string.Format("{0}", extraDamageMultiplierPerAmount * 100f)
			});
		}
	}
}
