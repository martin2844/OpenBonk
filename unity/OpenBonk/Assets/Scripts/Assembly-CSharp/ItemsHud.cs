using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class ItemsHud : MonoBehaviour
{
	public GameObject prefab;

	private Dictionary<EItem, ItemsHudElementPrefab> itemToPrefab = new Dictionary<EItem, ItemsHudElementPrefab>();

	private void Start()
	{
		ItemInventory.A_ItemAdded += OnItemAdded;
		ItemInventory.A_ItemRemoved += OnItemRemoved;
		MyPlayer.A_PlayerInventoryInitialized += OnInventoryInitialized;
		OnInventoryInitialized(MyPlayer.Instance != null ? MyPlayer.Instance.inventory : null);
	}

	private void OnDestroy()
	{
		ItemInventory.A_ItemAdded -= OnItemAdded;
		ItemInventory.A_ItemRemoved -= OnItemRemoved;
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInitialized;
	}

	private void OnInventoryInitialized(PlayerInventory inventory)
	{
		if (inventory == null || inventory.itemInventory == null) return;
		foreach (EItem item in inventory.itemInventory.items.Keys) OnItemAdded(item);
	}

	private void OnItemAdded(EItem item)
	{
		if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.itemInventory == null || prefab == null) return;
		if (!itemToPrefab.TryGetValue(item, out var entry))
		{
			entry = Instantiate(prefab, prefab.transform.parent).GetComponent<ItemsHudElementPrefab>();
			entry.gameObject.SetActive(true);
			itemToPrefab.Add(item, entry);
			entry.Set(DataManager.Instance.GetItem(item));
		}
		entry.SetAmount(MyPlayer.Instance.inventory.itemInventory.GetAmount(item));
	}

	private void OnItemRemoved(EItem item, bool showEffect)
	{
		if (!itemToPrefab.TryGetValue(item, out var entry)) return;
		int amount = MyPlayer.Instance.inventory.itemInventory.GetAmount(item);
		if (amount > 0) entry.SetAmount(amount);
		else { itemToPrefab.Remove(item); Destroy(entry.gameObject); }
	}
}
