using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBrassKnuckles : ItemBase
	{
		private float damagePerAmount = .25f;

		private float flatValue;

		private float radius;

		private float baseRadius = 8f;

		private float radiusAddPerAmount = 2f;

		protected override void OnInitOrAmountChanged()
		{
			radius = baseRadius + amount * radiusAddPerAmount;
			flatValue = amount * damagePerAmount;
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
			if (dc.enemy.enemyMovement.distanceToTarget <= radius) itemAttackModifier.AddFlat(flatValue);
		}

		public override bool HasPreAttackProc()
		{
			return true;
		}

		public ItemBrassKnuckles(ItemInventory itemInventoryRef)
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("+{0}%", damagePerAmount * 100f) };
		}
	}
}
