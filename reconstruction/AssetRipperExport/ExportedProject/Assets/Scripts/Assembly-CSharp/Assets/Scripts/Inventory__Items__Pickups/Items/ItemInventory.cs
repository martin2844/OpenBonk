using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public class ItemInventory
	{
		public Dictionary<EItem, ItemBase> items = new Dictionary<EItem, ItemBase>();

		public static Action<EStat> A_StatsChanged;

		public static Action<EItem> A_ItemAdded;

		public static Action<EItem, bool> A_ItemRemoved;

		private HashSet<EItem> itemsWithOnHitProcs = new HashSet<EItem>();

		private HashSet<EItem> itemsWithPreAttackProcs = new HashSet<EItem>();

		private List<EItem> itemsWithOnHitSorted = new List<EItem>();

		private List<EItem> itemsSorted = new List<EItem>();

		private Dictionary<EItem, string> itemToStringCache = new Dictionary<EItem, string>();

		private DamageContainer postDamageDc = new DamageContainer(0f, "");

		public ItemInventory()
		{
			LateFixedUpdate.A_LateUpdate += OnLateFixedUpdate;
		}

		public void AddItem(EItem eItem)
		{
			if (ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS) && eItem != EItem.CryptKey && eItem != EItem.CageKey) return;
			if (!items.TryGetValue(eItem, out ItemBase item))
			{
				item = ItemFactory.CreateItem(eItem, this);
				item.Init();
				items.Add(eItem, item);
			}
			item.AddAmount();
			if (!itemsSorted.Contains(eItem)) { itemsSorted.Add(eItem); SortItems(); }
			if (item.HasOnHitEffectProc() && itemsWithOnHitProcs.Add(eItem)) { itemsWithOnHitSorted.Add(eItem); SortOnHitItems(); }
			if (item.HasPreAttackProc()) itemsWithPreAttackProcs.Add(eItem);
			A_ItemAdded?.Invoke(eItem);
		}

		public void AddItem(EItem eItem, int count)
		{
			for (int i = 0; i < count; i++) AddItem(eItem);
		}

		public ItemBase GetItem(EItem eItem)
		{
			return items.TryGetValue(eItem, out ItemBase item) ? item : null;
		}

		public void RemoveItem(EItem eItem, bool showEffect = true)
		{
			if (!items.TryGetValue(eItem, out ItemBase item)) return;
			item.RemoveAmount();
			if (item.amount <= 0)
			{
				if (item.HasOnHitEffectProc() && itemsWithOnHitProcs.Remove(eItem) && itemsWithOnHitSorted.Remove(eItem)) SortOnHitItems();
				if (item.HasPreAttackProc()) itemsWithPreAttackProcs.Remove(eItem);
				item.Cleanup();
				items.Remove(eItem);
				if (itemsSorted.Remove(eItem)) SortItems();
			}
			A_ItemRemoved?.Invoke(eItem, showEffect);
		}

		private void SortItems()
		{
			itemsSorted.Sort((a, b) => DataManager.Instance.GetItem(a).itemTickPriority.CompareTo(DataManager.Instance.GetItem(b).itemTickPriority));
		}

		private void SortOnHitItems()
		{
			itemsWithOnHitSorted.Sort((a, b) => DataManager.Instance.GetItem(a).itemTickPriority.CompareTo(DataManager.Instance.GetItem(b).itemTickPriority));
		}

		public void Tick()
		{
			if (MyTime.paused) return;
			foreach (EItem item in itemsSorted) items[item].Tick();
		}

		public StatComponents PreAttack(DamageContainer dc, StatComponents itemModifierStatComponents)
		{
			itemModifierStatComponents.Recycle();
			foreach (EItem item in itemsWithPreAttackProcs)
				items[item].PreAttack(dc, itemModifierStatComponents);
			return itemModifierStatComponents;
		}

		public void PostDamage(DamageContainer dc)
		{
			if (dc.procCoefficient <= 0f) return;
			postDamageDc.Copy(dc);
			int repetitions = 1 + GetAmount(EItem.OverpoweredLamp);
			for (int i = 0; i < repetitions; i++)
			{
				foreach (EItem item in itemsWithOnHitSorted)
				{
					if (dc.enemy == null) break;
					items[item].ProcOnHitEffects(postDamageDc);
				}
			}
		}

		public void BeforePlayerDamage(DamageContainer dc)
		{
			foreach (EItem item in itemsSorted)
				if (items[item] is OpenBonkStackingItem external) external.BeforePlayerDamage(dc);
		}

		private void OnLateFixedUpdate()
		{
			if (MyTime.paused) return;
			foreach (ItemBase item in items.Values) item.LateFixedUpdate();
		}

		public void StatWasModified(EStat stat)
		{
			A_StatsChanged?.Invoke(stat);
		}

		public int GetAmount(EItem eItem)
		{
			return items.TryGetValue(eItem, out ItemBase item) ? item.amount : 0;
		}

		public int GetUniqueItemsInRarity(EItemRarity itemRarity)
		{
			int count = 0;
			foreach (EItem item in items.Keys)
				if (DataManager.Instance.GetItem(item).rarity == itemRarity) count++;
			return count;
		}

		public void Cleanup()
		{
			LateFixedUpdate.A_LateUpdate -= OnLateFixedUpdate;
			foreach (ItemBase item in items.Values) item.Cleanup();
			itemsWithOnHitProcs.Clear();
		}
	}
}
