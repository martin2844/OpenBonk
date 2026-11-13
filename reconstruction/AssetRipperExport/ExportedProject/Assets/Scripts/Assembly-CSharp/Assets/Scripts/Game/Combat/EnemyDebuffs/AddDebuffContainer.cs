using Assets.Scripts.Actors;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs
{
	public struct AddDebuffContainer
	{
		public EDebuff eDebuff;

		public DamageContainer dc;

		public float duration;

		public int stacks;

		public AddDebuffContainer(EDebuff eDebuff, DamageContainer dc, float duration, int stacks = 1)
		{
			this.eDebuff = eDebuff;
			this.dc = dc;
			this.duration = duration;
			this.stacks = stacks;
		}
	}
}
