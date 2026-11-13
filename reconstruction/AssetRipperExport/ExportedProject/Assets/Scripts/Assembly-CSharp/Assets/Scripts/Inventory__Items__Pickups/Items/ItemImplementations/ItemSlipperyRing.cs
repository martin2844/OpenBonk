using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSlipperyRing : ItemBase
	{
		private float evasionPerStack = 0.15f;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.Evasion, modifyType = EStatModifyType.Flat, modification = amount * evasionPerStack });
		}

		public ItemSlipperyRing(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.Evasion) ?? "", ["value1"] = string.Format("+{0}%", evasionPerStack * 100f) });
		}
	}
}
