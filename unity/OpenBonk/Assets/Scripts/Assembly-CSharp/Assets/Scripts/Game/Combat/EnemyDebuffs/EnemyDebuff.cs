using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs
{
	public abstract class EnemyDebuff
	{
		protected Enemy enemy;

		public int ticksLeft { get; protected set; }

		public EnemyDebuff()
		{
		}

		public void Set(Enemy enemy, DamageContainer dc, float duration, int stacks = 1)
		{
			ticksLeft = 0;
			this.enemy = enemy;
			Refresh(duration, stacks);
			OnAdded();
		}

		public virtual void Tick()
		{
			MyTick();
			ticksLeft--;
		}

		public bool IsDone()
		{
			return ticksLeft <= 0;
		}

		public void Refresh(float duration, int stacks)
		{
			AddStacks(stacks);
			int ticks = GetTicks(duration);
			if (ticks > ticksLeft) ticksLeft = ticks;
			OnRefresh();
		}

		private int GetTicks(float duration)
		{
			return Mathf.RoundToInt(DebuffUtility.debuffTicksPerSecond * duration * PlayerStats.GetStat(EStat.DurationMultiplier));
		}

		private void RefreshTimeout(float duration)
		{
			ticksLeft = GetTicks(duration);
		}

		public void ResetState()
		{
			ticksLeft = 0;
			OnResetState();
		}

		public abstract void AddStacks(int numStacks);

		public abstract int GetStacks();

		public abstract void MyTick();

		public abstract EDebuff GetDebuffType();

		public abstract void OnRemove(bool fromDeath);

		public abstract void OnAdded();

		public abstract void OnRefresh();

		protected abstract void OnResetState();
	}
}
