namespace Assets.Scripts.Game.Combat.EnemyDebuffs
{
	public class DebuffEcho : EnemyDebuff
	{
		public override int GetStacks()
		{
			return 0;
		}

		public override void MyTick()
		{
		}

		public override EDebuff GetDebuffType()
		{
			return EDebuff.Echo;
		}

		public override void OnRemove(bool fromDeath)
		{
			enemy.ReleaseEcho();
		}

		protected override void OnResetState()
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
