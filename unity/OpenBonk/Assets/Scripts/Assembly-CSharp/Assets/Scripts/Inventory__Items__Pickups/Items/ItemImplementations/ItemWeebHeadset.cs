using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemWeebHeadset : ItemBase
	{
		public float charmChancePerAmount;

		private float durationPerAmount;

		private float charmDuration;

		private float charmChance;

		private int maxProcsPerTick;

		private int numProcsThisTick;

		public ItemWeebHeadset(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			charmChancePerAmount = 0.02f;
			durationPerAmount = 1.5f;
			maxProcsPerTick = 100;
		}

		protected override void OnInitOrAmountChanged()
		{
			charmDuration = 5f + amount * durationPerAmount;
			charmChance = Mathf.Max(0.02f, StatScaling.HyperbolicScaling(amount * charmChancePerAmount, 0.1f, 4f));
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (dc.enemy.IsDead() || dc.enemy.HasDebuff(EDebuff.Charm) || numProcsThisTick >= maxProcsPerTick) return;
			if (!ItemUtility.TryProc(dc.procCoefficient, charmChance)) return;
			dc.enemy.Charm(dc, charmDuration);
			numProcsThisTick++;
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public override void Tick()
		{
			numProcsThisTick = 0;
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}
	}
}
