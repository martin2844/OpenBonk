using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive
{
	public abstract class PassiveAbility
	{
		public Dictionary<EStat, StatModifiersContainer> statModifiers = new Dictionary<EStat, StatModifiersContainer>();

		public static Action<EStat> A_StatModified;

		protected void SetStat(StatModifier statModifier)
		{
			if (!statModifiers.ContainsKey(statModifier.stat))
				statModifiers.Add(statModifier.stat, new StatModifiersContainer());
			statModifiers[statModifier.stat].SetModifier(statModifier);
			A_StatModified?.Invoke(statModifier.stat);
		}

		public abstract void Init();

		public abstract void Cleanup();

		public abstract void Tick();

		public abstract EPassive GetPassiveType();

		public virtual string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString();
		}
	}
}
