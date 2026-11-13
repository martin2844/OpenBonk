using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemTimeBracelet : ItemBase
	{
		private float damagePerAmount = 0.08f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.XpIncreaseMultiplier, modifyType = EStatModifyType.Flat, modification = amount * damagePerAmount });
		}

		public ItemTimeBracelet(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.XpIncreaseMultiplier) ?? "", ["value1"] = string.Format("+{0}%", damagePerAmount * 100f), ["time_bracelet"] = Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("ItemsGray", "TimeBracelet_NAME") });
		}
	}
}
