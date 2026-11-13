namespace Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations
{
	public class DebuffIce : EnemyDebuff
	{
		public static float slowMultiplier = 0.3f;

		public static int numFrozenEnemies;

		public override int GetStacks()
		{
			return 0;
		}

		public override void MyTick()
		{
		}

		public override EDebuff GetDebuffType()
		{
			return EDebuff.Freeze;
		}

		public override void OnAdded()
		{
			numFrozenEnemies++;
			enemy.enemyMovement.FindNextPosition();
		}

		public override void OnRemove(bool fromDeath)
		{
			numFrozenEnemies--;
			enemy.enemyMovement.FindNextPosition();
		}

		protected override void OnResetState()
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
