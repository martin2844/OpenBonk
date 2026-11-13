namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public class TemporaryStat
	{
		public StatModifier modifier;

		public float expirationTime;

		public TemporaryStat(StatModifier modifier, float expirationTime)
		{
			this.modifier = modifier;
			this.expirationTime = expirationTime;
		}
	}
}
