using System;

namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	[Serializable]
	public class StatCategoryRatio
	{
		public EStatCategory category;

		public float value;

		public StatCategoryRatio(EStatCategory category, float value)
		{
		}
	}
}
