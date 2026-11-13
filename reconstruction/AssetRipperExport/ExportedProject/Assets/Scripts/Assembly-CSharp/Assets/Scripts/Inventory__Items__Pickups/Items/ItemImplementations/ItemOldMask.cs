using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemOldMask : ItemBase
	{
		private float eliteSpawnChancePerAmount = .15f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.EliteSpawnIncrease, modifyType = EStatModifyType.Flat, modification = amount * eliteSpawnChancePerAmount });
		}

		public ItemOldMask(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.EliteSpawnIncrease) ?? "", ["value1"] = string.Format("+{0}%", eliteSpawnChancePerAmount * 100f) });
		}
	}
}
