using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemLeechingCrystal : ItemBase
	{
		private float regenAdditivePerAmount = -0.5f;

		private float maxHpPerAmount = 50f;

		public static string damageSource;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = maxHpPerAmount * amount });
			SetStat(new StatModifier { stat = EStat.HealthRegen, modifyType = EStatModifyType.Addition, modification = regenAdditivePerAmount * amount });
		}

		public ItemLeechingCrystal(ItemInventory itemInventoryRef)
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
				["stat1"] = EnumUtility.EnumToReadable(EStat.MaxHealth), ["value1"] = string.Format("+{0}", maxHpPerAmount),
				["stat2"] = EnumUtility.EnumToReadable(EStat.HealthRegen), ["value2"] = string.Format("{0}%", regenAdditivePerAmount * 100f)
			};
		}
	}
}
