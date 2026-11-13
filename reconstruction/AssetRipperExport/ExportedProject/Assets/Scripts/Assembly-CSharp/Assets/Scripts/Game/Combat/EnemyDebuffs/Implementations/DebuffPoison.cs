using Assets.Scripts.Actors;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations
{
	public class DebuffPoison : EnemyDebuff
	{
		public static int numPoisonedEnemies;

		private int stacks;

		public static string poisonDamageSource = EDebuff.Poison.ToString();

		public float GetDamageForHpBar()
		{
			return Mathf.Max(stacks, GetPoisonDamagePerTick(stacks)) * ticksLeft;
		}

		public override void MyTick()
		{
			var dc = new DamageContainer(0f, poisonDamageSource);
			dc.damage = Mathf.Max(stacks, GetPoisonDamagePerTick(stacks));
			dc.enemy = enemy;
			dc.damageEffect = EDamageEffect.Poison;
			enemy.DamageFromPlayerOther(dc);
		}

		public static float GetPoisonDamagePerTick(int stacks)
		{
			return stacks * PlayerStats.GetStat(EStat.DamageMultiplier) * PlayerStats.GetStat(EStat.PoisonDamageMultiplier);
		}

		public override EDebuff GetDebuffType()
		{
			return EDebuff.Poison;
		}

		public override void OnRemove(bool fromDeath)
		{
			numPoisonedEnemies--;
		}

		public override void OnAdded()
		{
			numPoisonedEnemies++;
		}

		protected override void OnResetState()
		{
			stacks = 0;
		}

		public override void OnRefresh()
		{
		}

		public override void AddStacks(int numStacks)
		{
			stacks += numStacks;
		}

		public override int GetStacks()
		{
			return stacks;
		}
	}
}
