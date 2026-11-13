using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public class StatModifiersContainer
	{
		private Dictionary<EStatModifyType, StatModifier> statContainers = new Dictionary<EStatModifyType, StatModifier>();

		public void SetModifier(StatModifier statModifier)
		{
			statContainers[statModifier.modifyType] = statModifier;
		}

		public IEnumerable<StatModifier> GetModifiers()
		{
			return statContainers.Values;
		}
	}
}
