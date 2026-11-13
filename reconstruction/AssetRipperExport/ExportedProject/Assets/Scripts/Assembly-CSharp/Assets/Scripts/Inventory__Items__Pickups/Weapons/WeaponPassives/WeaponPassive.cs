using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponPassives
{
	public abstract class WeaponPassive
	{
		public Dictionary<EStat, StatModifiersContainer> statModifiers = new Dictionary<EStat, StatModifiersContainer>();

		protected WeaponBase weaponBase;

		public WeaponPassive(WeaponBase weaponBase)
		{
			this.weaponBase = weaponBase;
		}

		protected void SetStat(StatModifier statModifier)
		{
			if (!statModifiers.TryGetValue(statModifier.stat, out var container))
				statModifiers[statModifier.stat] = container = new StatModifiersContainer();
			container.SetModifier(statModifier);
			weaponBase.WeaponPassiveChanged(statModifier.stat);
		}

		public abstract void Init();

		public abstract void Cleanup();
	}
}
