using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public abstract class ItemBase
	{
		private string damageSource;

		public int amount;

		private ItemInventory itemInventoryRef;

		public Dictionary<EStat, StatModifiersContainer> statModifiers;

		public static Action<ItemBase> A_ItemAdded;

		public static Action<ItemBase> A_ItemRemoved;

		protected ItemBase(ItemInventory itemInventoryRef)
		{
			this.itemInventoryRef = itemInventoryRef;
			statModifiers = new Dictionary<EStat, StatModifiersContainer>();
		}

		protected void SetStat(StatModifier statModifier)
		{
			if (itemInventoryRef == null) return;
			if (!statModifiers.TryGetValue(statModifier.stat, out StatModifiersContainer container))
			{
				container = new StatModifiersContainer();
				statModifiers.Add(statModifier.stat, container);
			}
			container.SetModifier(statModifier);
			itemInventoryRef.StatWasModified(statModifier.stat);
		}

		public void AddAmount()
		{
			amount++;
			OnInitOrAmountChanged();
			A_ItemAdded?.Invoke(this);
		}

		public void RemoveAmount()
		{
			amount--;
			OnInitOrAmountChanged();
			A_ItemRemoved?.Invoke(this);
		}

		public abstract void Init();

		public abstract void Cleanup();

		protected abstract void OnInitOrAmountChanged();

		public abstract void Tick();

		public abstract void PreAttack(DamageContainer dc, StatComponents itemAttackModifier);

		public abstract void ProcOnHitEffects(DamageContainer dc);

		public abstract bool HasOnHitEffectProc();

		public abstract bool HasPreAttackProc();

		public virtual void LateFixedUpdate()
		{
		}

		protected virtual Dictionary<string, object> GetLocalizationKeys()
		{
			return null;
		}

		public virtual string GetDescription(LocalizedString localizedString)
		{
			var keys = GetLocalizationKeys();
			return keys != null && keys.Count > 0 ? localizedString.GetLocalizedString(keys) : localizedString.GetLocalizedString();
		}
	}
}
