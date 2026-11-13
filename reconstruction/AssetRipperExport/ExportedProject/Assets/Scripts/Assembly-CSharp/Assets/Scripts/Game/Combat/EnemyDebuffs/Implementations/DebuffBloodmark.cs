using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations
{
	public class DebuffBloodmark : EnemyDebuff
	{
		public const float defaultDuration = 4f;

		private int stacks;

		public static string damageSource = EDebuff.Bloodmark.ToString();

		private DamageContainer dc = new DamageContainer(0f, damageSource);

		private float baseDamageMultiplier = .75f;

		private float damage;

		private bool isDone;

		private bool isSubscribed;

		public override int GetStacks()
		{
			return stacks;
		}

		public override void MyTick()
		{
		}

		public float GetDamage()
		{
			return damage;
		}

		public override void AddStacks(int numStacks)
		{
			stacks += numStacks;
			damage = MyPlayer.Instance.GetBaseDamage() * baseDamageMultiplier * stacks * PlayerStats.GetStat(EStat.DamageMultiplier);
			if (damage >= enemy.hp)
			{
				ticksLeft = 0;
				isDone = true;
				dc.Reuse(0f, damageSource);
				dc.damage = damage;
				dc.enemy = enemy;
				dc.damageEffect = EDamageEffect.Bloodmark;
				enemy.DamageFromPlayerOther(dc);
				stacks = 0;
				damage = 0f;
			}
		}

		public override EDebuff GetDebuffType()
		{
			return EDebuff.Bloodmark;
		}

		public override void OnRemove(bool fromDeath)
		{
			if (isSubscribed) { enemy.A_DamageNonStatic -= OnEnemyDamaged; isSubscribed = false; }
			if (stacks <= 0 || fromDeath || enemy.hp <= 0f) return;
			dc.damage = damage;
			dc.enemy = enemy;
			dc.damageEffect = EDamageEffect.Bloodmark;
			enemy.DamageFromPlayerOther(dc);
		}

		public override void OnAdded()
		{
			if (isSubscribed) return;
			isDone = false;
			isSubscribed = true;
			enemy.A_DamageNonStatic += OnEnemyDamaged;
		}

		private void OnEnemyDamaged(Enemy e, DamageContainer dc)
		{
			if (e == enemy && !enemy.IsDeadOrDyingNextFrame() && damage >= e.hp) e.debuffsToRemove.Add(EDebuff.Bloodmark);
		}

		public override void OnRefresh()
		{
			if (isDone) ticksLeft = 0;
		}

		protected override void OnResetState()
		{
			stacks = 0;
			damage = 0f;
			isDone = false;
		}
	}
}
