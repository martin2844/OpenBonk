using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.UI.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemUnstableTransfusion : ItemBase
	{
		private float chanceToStackPerAmount = .35f;

		private float totalChance;

		protected override void OnInitOrAmountChanged()
		{
			totalChance = amount * chanceToStackPerAmount;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (!ItemUtility.TryProc(dc.procCoefficient, totalChance)) return;
			int stacks = chanceToStackPerAmount > 0f ? Mathf.FloorToInt(totalChance) : 0;
			if (ItemUtility.TryProc(dc.procCoefficient, totalChance - stacks)) stacks++;
			dc.enemy.AddDebuff(EDebuff.Bloodmark, dc, 5f, stacks);
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public ItemUnstableTransfusion(ItemInventory itemInventoryRef)
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

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["value1"] = string.Format("+{0}%", chanceToStackPerAmount * 100f),
				["bloodmark"] = LocalizationUtility.GetLocalizedString("DamageSources", EDebuff.Bloodmark.ToString()) ?? ""
			});
		}
	}
}
