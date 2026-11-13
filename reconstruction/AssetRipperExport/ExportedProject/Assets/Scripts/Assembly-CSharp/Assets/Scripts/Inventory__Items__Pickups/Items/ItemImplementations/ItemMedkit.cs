using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using Assets.Scripts.Menu.Shop;
using System.Collections.Generic;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemMedkit : ItemBase
	{
		private float hpRegenPerAmount = 45f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.HealthRegen, modifyType = EStatModifyType.Flat, modification = amount * hpRegenPerAmount });
		}

		public ItemMedkit(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.HealthRegen), ["value1"] = string.Format("+{0}", hpRegenPerAmount) });
		}
	}
}
