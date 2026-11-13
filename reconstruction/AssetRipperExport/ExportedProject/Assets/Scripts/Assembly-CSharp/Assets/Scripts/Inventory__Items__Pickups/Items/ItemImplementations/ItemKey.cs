using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemKey : ItemBase
	{
		private float chancePerStack = 0.1f;

		private float currentChance = 0.1f;

		protected override void OnInitOrAmountChanged()
		{
			currentChance = StatScaling.HyperbolicScaling(amount * chancePerStack, 1f, 1f);
		}

		public float GetChance()
		{
			return currentChance;
		}

		public ItemKey(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("+{0}%", chancePerStack * 100f) });
		}
	}
}
