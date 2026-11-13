using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesActives
{
	public abstract class ActiveAbility
	{
		protected float readyAtTime;

		public void Use()
		{
			if (!IsReady()) return;
			readyAtTime = MyTime.time + GetCooldown();
			UseImplementation();
		}

		public abstract void Tick();

		private bool IsReady()
		{
			return MyTime.time >= readyAtTime;
		}

		public abstract void Init();

		public abstract void Cleanup();

		public abstract void UseImplementation();

		public abstract float GetCooldown();
	}
}
