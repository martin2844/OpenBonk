using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.UI.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemWrench : ItemBase
	{
		private float chargeSpeedIncrease = 0.04f;

		private float chargeRewardIncrease = 0.075f;

		public float GetChargeSpeedIncrease()
		{
			return amount * chargeSpeedIncrease;
		}

		public float GetChargeRewardMultiplier()
		{
			return 1f + amount * chargeRewardIncrease;
		}

		protected override void OnInitOrAmountChanged()
		{
		}

		public ItemWrench(ItemInventory itemInventoryRef)
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
			var values = new Dictionary<string, string>
			{
				["charge_shrine"] = LocalizationUtility.GetLocalizedString("Game_Interactables", "CHARGE_SHRINE_NAME"),
				["value1"] = string.Format("{0}", chargeSpeedIncrease * 100f),
				["value2"] = string.Format("{0}%", chargeRewardIncrease * 100f)
			};
			return localizedString.GetLocalizedString(values);
		}
	}
}
