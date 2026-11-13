using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations
{
	public class DebuffFire : EnemyDebuff
	{
		private static string damageSource = EDebuff.Burn.ToString();

		private DamageContainer dc = new DamageContainer(0f, damageSource);

		private bool canDamage;

		public override int GetStacks()
		{
			return 0;
		}

		public override void MyTick()
		{
			bool skip = canDamage;
			canDamage = !canDamage;
			if (skip) return;
			dc.Reuse(0f, damageSource);
			dc.damage = GetDamage();
			dc.enemy = enemy;
			dc.damageEffect = EDamageEffect.Fire;
			dc.element = EElement.Fire;
			enemy.DamageFromPlayerOther(dc);
		}

		private float GetDamage()
		{
			return MyPlayer.Instance.GetBaseDamage() * 0.85f * PlayerStats.GetStat(EStat.DamageMultiplier);
		}

		protected override void OnResetState()
		{
			canDamage = false;
		}

		public override EDebuff GetDebuffType()
		{
			return EDebuff.Burn;
		}

		public override void OnRemove(bool fromDeath)
		{
		}

		public override void OnAdded()
		{
		}

		public override void OnRefresh()
		{
		}

		public override void AddStacks(int numStacks)
		{
		}
	}
}
