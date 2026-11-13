namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public class StatusEffect
	{
		public EStatusEffect eStatusEffect;

		public StatModifier[] modifiers;

		public float expirationTime;

		public float addedTime;

		public StatusEffect(EStatusEffect eStatusEffect, float expirationTime, StatModifier[] modifiers)
		{
			this.eStatusEffect = eStatusEffect;
			this.expirationTime = expirationTime;
			this.modifiers = modifiers;
			addedTime = Assets.Scripts.Utility.MyTime.time;
		}
	}
}
