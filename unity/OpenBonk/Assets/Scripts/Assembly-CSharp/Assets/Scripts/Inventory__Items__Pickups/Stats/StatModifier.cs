using System;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	[Serializable]
	public class StatModifier
	{
		public EStat stat;

		public EStatModifyType modifyType;

		public float modification;

		public float GetModificationAtAmount(int amount)
		{
			return modification;
		}

		public float GetModificationTotal(int amount)
		{
			float total = 0f;
			if (unchecked(amount + 1) < 2) return total;
			for (int i = 0; i < amount; i++) total += modification;
			return total;
		}

		public override string ToString()
		{
			return string.Format("{0}: {1} ({2})\n", stat, modification, modifyType);
		}
	}
}
