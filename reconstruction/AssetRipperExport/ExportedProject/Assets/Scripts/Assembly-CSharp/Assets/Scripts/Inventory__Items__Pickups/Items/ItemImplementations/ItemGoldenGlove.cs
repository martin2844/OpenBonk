using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGoldenGlove : ItemBase
	{
		private float chancePerAmount = 0.15f;

		private float chance;

		private int extraGoldFromOverload;

		protected override void OnInitOrAmountChanged()
		{
			chance = amount * chancePerAmount;
			if (chance > 1f) { extraGoldFromOverload = Mathf.FloorToInt(chance); chance -= extraGoldFromOverload; }
		}

		public int GetExtraGold()
		{
			return extraGoldFromOverload + (global::Utility.MyRandom.random.NextDouble() < chance ? 1 : 0);
		}

		public ItemGoldenGlove(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("+{0}%", chancePerAmount * 100f) });
		}
	}
}
