using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemWizardsHat : ItemBase
	{
		private int tomeLevelsPerAmountMax = 10;

		private int tomeLevelsPerAmountMin = 2;

		private int startFalloffAtAmount = 1;

		public int tomeLevels;

		protected override void OnInitOrAmountChanged()
		{
			tomeLevels += GetLevelsForAmount(tomeLevelsPerAmountMin, tomeLevelsPerAmountMax);
		}

		private int GetLevelsForAmount(int minPerAmount, int maxPerAmount)
		{
			int beyondStart = amount - startFalloffAtAmount;
			return beyondStart > 0 ? Mathf.Max(minPerAmount, maxPerAmount - beyondStart) : maxPerAmount;
		}

		public ItemWizardsHat(ItemInventory itemInventoryRef)
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["value1"] = string.Format("{0}", tomeLevelsPerAmountMax),
				["value2"] = string.Format("{0}", tomeLevelsPerAmountMin),
				["value3"] = string.Format("{0}", tomeLevelsPerAmountMax - tomeLevelsPerAmountMin + startFalloffAtAmount)
			});
		}
	}
}
