using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemTacticalGlasses : ItemBase
	{
		private float damagePerAmount = .2f;

		private float additiveDamage;

		protected override void OnInitOrAmountChanged()
		{
			additiveDamage = amount * damagePerAmount;
		}

		public ItemTacticalGlasses(ItemInventory itemInventoryRef)
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
			if (dc.enemy != null && dc.enemy.GetHpRatio() >= .9f) itemAttackModifier.AddAdditive(additiveDamage);
		}

		public override bool HasPreAttackProc()
		{
			return true;
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
			return localizedString.GetLocalizedString(new Dictionary<string, string> { ["value1"] = string.Format("+{0}%", damagePerAmount * 100f) });
		}
	}
}
