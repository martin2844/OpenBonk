using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemChonkplate : ItemBase
	{
		private float overhealPerAmount = 0.75f;

		private float lifestealPerAmount = 0.2f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.Overheal, modifyType = EStatModifyType.Flat, modification = amount * overhealPerAmount });
			SetStat(new StatModifier { stat = EStat.Lifesteal, modifyType = EStatModifyType.Flat, modification = amount * lifestealPerAmount });
		}

		public ItemChonkplate(ItemInventory itemInventoryRef)
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(Assets.Scripts.Menu.Shop.EStat.MaxHealth) ?? "", ["stat2"] = EnumUtility.EnumToReadable(Assets.Scripts.Menu.Shop.EStat.Overheal) ?? "", ["stat3"] = EnumUtility.EnumToReadable(Assets.Scripts.Menu.Shop.EStat.Lifesteal) ?? "", ["value1"] = string.Format("{0}%", overhealPerAmount * 100f), ["value2"] = string.Format("{0}%", lifestealPerAmount * 100f) };
		}
	}
}
