using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.UI.InGame.Rewards;
using TMPro;
using UnityEngine;

public class MicrowaveUi : BaseEncounterWindow
{
	public GameObject itemPrefab;

	private List<MicrowaveItemButton> itemPrefabs;

	public TextMeshProUGUI t_price;

	public override void Open(EEncounter encounterType)
	{
		gameObject.SetActive(true);
		var microwave = InteractableMicrowave.currentlyInteracting;
		var inventory = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null ? MyPlayer.Instance.inventory.itemInventory : null;
		if (microwave == null || inventory == null || DataManager.Instance == null || itemPrefab == null) return;
		if (itemPrefabs == null) itemPrefabs = new List<MicrowaveItemButton> { itemPrefab.GetComponent<MicrowaveItemButton>() };
		var matchingItems = new List<EItem>();
		foreach (var item in inventory.items.Keys)
			if (DataManager.Instance.GetItem(item).rarity == microwave.rarity) matchingItems.Add(item);
		foreach (var button in itemPrefabs) if (button != null) button.gameObject.SetActive(false);
		for (int i = 0; i < matchingItems.Count; i++)
		{
			if (i >= itemPrefabs.Count) itemPrefabs.Add(Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<MicrowaveItemButton>());
			var button = itemPrefabs[i];
			if (button == null) continue;
			button.gameObject.SetActive(true);
			button.Set(this, matchingItems[i]);
		}
		if (t_price != null) t_price.text = string.Format("<size=110%><sprite name=gold></size> {0}", microwave.GetPrice());
	}

	private void Update()
	{
		if (gameObject.activeInHierarchy && MyInputManager.GetButtonDown(MyInputManager.UICancel)) CloseScreen();
	}

	public void SelectUpgrade(EItem eItem)
	{
		if (InteractableMicrowave.currentlyInteracting == null) return;
		InteractableMicrowave.currentlyInteracting.UseMicrowave(eItem);
		CloseScreen();
	}

	public void CloseScreen()
	{
		if (UiManager.Instance != null && UiManager.Instance.encounterWindows != null) UiManager.Instance.encounterWindows.RewardFinished();
	}

	public override void OnClose()
	{
		gameObject.SetActive(false);
	}

	public override void ChooseOffer(int index)
	{
	}
}
