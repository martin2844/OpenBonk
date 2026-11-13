using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemPumpkin : ItemBase
	{
		private int extraPotsPerAmount = 8;

		private float rewardMultiplierPerAmount = 0.18f;

		public float GetRewardMultiplier()
		{
			return 1f + amount * rewardMultiplierPerAmount;
		}

		public int GetExtraPotsSmall()
		{
			return amount * extraPotsPerAmount;
		}

		public int GetExtraPotsBig()
		{
			return UnityEngine.Mathf.RoundToInt(amount * extraPotsPerAmount * 0.25f);
		}

		public ItemPumpkin(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		protected override void OnInitOrAmountChanged()
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value"] = string.Format("+{0}", extraPotsPerAmount), ["value2"] = string.Format("{0}%", rewardMultiplierPerAmount * 100f) });
		}
	}
}
