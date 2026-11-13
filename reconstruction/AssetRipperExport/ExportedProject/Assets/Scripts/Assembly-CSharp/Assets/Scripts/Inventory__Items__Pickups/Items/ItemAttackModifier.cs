using Assets.Scripts.Actors;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public class ItemAttackModifier
	{
		public float flatValues;

		public float additiveValues;

		public float multiplicativeValues = 1f;

		public float damageMultiplier { get; private set; } = 1f;

		public void Recycle()
		{
			flatValues = 0f;
			additiveValues = 0f;
			multiplicativeValues = 1f;
		}

		public void Apply(DamageContainer dc)
		{
			dc.damage *= damageMultiplier;
		}

		public void AddMultiplier(float multiplier)
		{
			damageMultiplier *= multiplier;
		}
	}
}
