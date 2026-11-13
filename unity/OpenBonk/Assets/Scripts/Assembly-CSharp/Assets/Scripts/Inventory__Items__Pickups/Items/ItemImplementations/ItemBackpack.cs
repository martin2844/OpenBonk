using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBackpack : ItemBase
	{
		private int projectilesPerAmount = 1;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.Projectiles, modifyType = EStatModifyType.Flat, modification = amount * projectilesPerAmount });
		}

		public ItemBackpack(ItemInventory itemInventoryRef)
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(Assets.Scripts.Menu.Shop.EStat.Projectiles) ?? "", ["value1"] = string.Format("+{0}", projectilesPerAmount) };
		}
	}
}
