using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine.Localization;
using System.Collections.Generic;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemClover : ItemBase
	{
		private float luckPerAmount = 0.1f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.Luck, modifyType = EStatModifyType.Flat, modification = amount * luckPerAmount });
		}

		public ItemClover(ItemInventory itemInventoryRef)
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
			var keys = new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.Luck), ["value1"] = string.Format("+{0}%", luckPerAmount * 100f) };
			return localizedString.GetLocalizedString(keys);
		}
	}
}
