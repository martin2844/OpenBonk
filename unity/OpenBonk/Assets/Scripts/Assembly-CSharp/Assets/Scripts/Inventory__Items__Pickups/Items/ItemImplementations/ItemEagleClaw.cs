using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemEagleClaw : ItemBase
	{
		public float procChancePerAmount = .08f;

		private float procChance;

		public float damageAddition = .66f;

		public float damageAdditionPerAmount = .66f;

		private float knockupForce = 3.5f;

		protected override void OnInitOrAmountChanged()
		{
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, 1f, .5f);
			damageAddition = amount * damageAdditionPerAmount;
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
			if (dc.enemy?.enemyMovement?.grounded != true) itemAttackModifier.AddAdditive(damageAddition);
		}

		public override bool HasPreAttackProc()
		{
			return true;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (!dc.enemy.enemyMovement.grounded || !ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			dc.enemy.enemyMovement.KnockUp(knockupForce * PlayerStats.GetStat(EStat.KnockbackMultiplier));
			dc.enemy.enemyMovement.Knockback(dc.direction, knockupForce * PlayerStats.GetStat(EStat.KnockbackMultiplier));
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public ItemEagleClaw(ItemInventory itemInventoryRef)
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "", ["value1"] = string.Format("{0}%", damageAdditionPerAmount * 100f), ["value2"] = string.Format("{0}%", procChancePerAmount * 100f) };
		}
	}
}
