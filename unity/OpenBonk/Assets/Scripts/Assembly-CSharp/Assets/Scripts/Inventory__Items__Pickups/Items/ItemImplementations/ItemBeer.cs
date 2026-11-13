using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBeer : ItemBase
	{
		private float damagePerStack = 0.2f;

		private float maxHealthPerStack = -0.05f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = amount * damagePerStack });
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Addition, modification = amount * maxHealthPerStack });
		}

		public ItemBeer(ItemInventory itemInventoryRef)
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
			return new Dictionary<string, object> {
				["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier), ["value1"] = string.Format("+{0}%", damagePerStack * 100f),
				["stat2"] = EnumUtility.EnumToReadable(EStat.MaxHealth), ["value2"] = string.Format("{0}%", maxHealthPerStack * 100f)
			};
		}
	}
}
