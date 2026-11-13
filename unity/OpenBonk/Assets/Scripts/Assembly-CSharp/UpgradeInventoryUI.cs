using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;

public class UpgradeInventoryUI : MonoBehaviour
{
	public GameObject itemContainerPrefab;

	public Transform weaponParent;

	public Transform tomeParent;

	public Transform itemParent;

	public Transform banishedItemsParent;

	private List<InventoryItemPrefabUI> weaponContainers = new List<InventoryItemPrefabUI>();

	private List<InventoryItemPrefabUI> tomeContainers = new List<InventoryItemPrefabUI>();

	private List<InventoryItemPrefabUI> itemContainers = new List<InventoryItemPrefabUI>();

	private List<InventoryItemPrefabUI> banishedItemContainers = new List<InventoryItemPrefabUI>();

	private void OnEnable()
	{
		Refresh();
	}

	public void Refresh()
	{
		if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || itemContainerPrefab == null) return;
		var inventory = MyPlayer.Instance.inventory;
		var weapons = inventory.weaponInventory.weapons.Values.ToList();
		var tomes = inventory.tomeInventory.tomeLevels.Keys.ToList();
		var items = inventory.itemInventory.items.Keys.ToList();
		int weaponSlots = Mathf.Max(InventoryUtility.GetNumAvailableWeaponSlots(), weapons.Count);
		int tomeSlots = Mathf.Max(InventoryUtility.GetNumAvailableTomeSlots(), tomes.Count);
		for (int i = 0; i < InventoryUtility.GetNumMaxWeaponSlots(); i++)
		{
			var entry = Entry(weaponContainers, weaponParent, i);
			if (i >= weaponSlots) entry.SetUnavailable();
			else entry.SetItem(i < weapons.Count ? weapons[i].weaponData : null);
		}
		for (int i = 0; i < InventoryUtility.GetNumMaxTomeSlots(); i++)
		{
			var entry = Entry(tomeContainers, tomeParent, i);
			if (i >= tomeSlots) entry.SetUnavailable();
			else entry.SetItem(i < tomes.Count ? DataManager.Instance.GetTome(tomes[i]) : null);
		}
		foreach (var entry in itemContainers) entry.gameObject.SetActive(false);
		for (int i = 0; i < items.Count; i++) Entry(itemContainers, itemParent, i).SetItem(items[i]);
		if (banishedItemsParent != null)
		{
			var banished = RunUnlockables.banishedItems;
			banishedItemsParent.gameObject.SetActive(banished != null && banished.Count > 0);
			if (banished != null && banished.Count > 0)
			{
				foreach (var entry in banishedItemContainers) entry.gameObject.SetActive(false);
				int index = 0;
				foreach (var item in banished)
				{
					var entry = Entry(banishedItemContainers, banishedItemsParent, index++);
					entry.SetItem(item);
					entry.SetBanished();
				}
			}
		}
		Rebuild();
		Invoke(nameof(Rebuild), 0.01f);
	}

	private void Rebuild()
	{
		Assets.Scripts.UI.UiUtility.RebuildUi(transform);
	}

	private InventoryItemPrefabUI Entry(List<InventoryItemPrefabUI> entries, Transform parent, int index)
	{
		if (index >= entries.Count) entries.Add(Instantiate(itemContainerPrefab, parent).GetComponent<InventoryItemPrefabUI>());
		var entry = entries[index];
		entry.gameObject.SetActive(true);
		return entry;
	}
}
