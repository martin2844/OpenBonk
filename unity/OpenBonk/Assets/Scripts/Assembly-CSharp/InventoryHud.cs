using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.Tomes;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;

public class InventoryHud : MonoBehaviour
{
	public GameObject itemContainerPrefab;

	public Transform weaponParent;

	public Transform tomeParent;

	private List<InventoryItemPrefabUI> weaponContainers;

	private List<InventoryItemPrefabUI> tomeContainers;

	private void Start()
	{
		weaponContainers = new List<InventoryItemPrefabUI>();
		tomeContainers = new List<InventoryItemPrefabUI>();
		MyPlayer.A_PlayerInventoryInitialized += OnInventoryInit;
		Assets.Scripts.Inventory__Items__Pickups.TomeInventory.A_TomeUpgrade += OnTomeAdded;
		WeaponInventory.A_WeaponAdded += OnWeaponAdded;
		Refresh();
	}

	private void OnDestroy()
	{
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInit;
		Assets.Scripts.Inventory__Items__Pickups.TomeInventory.A_TomeUpgrade -= OnTomeAdded;
		WeaponInventory.A_WeaponAdded -= OnWeaponAdded;
	}

	private void OnInventoryInit(PlayerInventory obj)
	{
		Refresh();
	}

	public void Refresh()
	{
		if (GameManager.Instance == null || GameManager.Instance.GetPlayerInventory() == null || itemContainerPrefab == null) return;
		PlayerInventory inventory = GameManager.Instance.GetPlayerInventory();
		foreach (var entry in weaponContainers) if (entry != null) entry.gameObject.SetActive(false);
		foreach (var entry in tomeContainers) if (entry != null) entry.gameObject.SetActive(false);
		int i = 0;
		foreach (var weapon in inventory.weaponInventory.weapons.Values)
			SetEntry(weaponContainers, weaponParent, i++, weapon.weaponData);
		i = 0;
		foreach (var tome in inventory.tomeInventory.tomeLevels.Keys)
			SetEntry(tomeContainers, tomeParent, i++, DataManager.Instance.GetTome(tome));
		SeparateRows();
	}

	private void SeparateRows()
	{
		if (!(weaponParent is RectTransform weapons) || !(tomeParent is RectTransform tomes) ||
			!(weapons.parent is RectTransform parent) || parent != tomes.parent) return;
		var weaponFitter = weapons.GetComponent<ContentSizeFitter>();
		var tomeFitter = tomes.GetComponent<ContentSizeFitter>();
		if (weaponFitter != null) weaponFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		if (tomeFitter != null) tomeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		// The parent places its children before their fitters resize; a second
		// pass positions the rows using their new heights.
		LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
		LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
	}

	private void SetEntry(List<InventoryItemPrefabUI> entries, Transform parent, int index, Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase item)
	{
		if (parent == null || item == null) return;
		if (index >= entries.Count)
			entries.Add(Instantiate(itemContainerPrefab, parent).GetComponent<InventoryItemPrefabUI>());
		if (entries[index] == null) return;
		entries[index].SetItem(item);
		entries[index].gameObject.SetActive(true);
	}

	private void OnTomeAdded(ETome eTome, EStat obj)
	{
		Refresh();
	}

	private void OnWeaponAdded(WeaponBase obj)
	{
		Refresh();
	}
}
