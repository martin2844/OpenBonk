using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;
using Assets.Scripts.Managers;

public class UnlocksUi : MonoBehaviour
{
	private List<UnlockContainer> unlockContainers;

	public GameObject unlockContainerPrefab;

	public Transform contentParent;

	public ButtonNavigationSelectionOnly tabButtons;

	public TabGridNavigation tabGridNavigation;

	public GameObject exclChar;

	public GameObject exclWeapon;

	public GameObject exclTome;

	public GameObject exclItem;

	public GameObject exclHats;

	public GameObject[] exclamationMarks;

	private void Awake()
	{
		if (SaveManager.Instance?.progression?.menuMeta != null)
			SaveManager.Instance.progression.menuMeta.hasVisitedUnlocks = true;
		unlockContainers = new List<UnlockContainer>();
		if (tabButtons != null) tabButtons.A_ButtonSelected += OnTabSelected;
		UnlockContainer.A_RemovedAlert += UpdateExclamationMarks;
	}

	private void OnDestroy()
	{
		if (tabButtons != null) tabButtons.A_ButtonSelected -= OnTabSelected;
		UnlockContainer.A_RemovedAlert -= UpdateExclamationMarks;
	}

	private void OnEnable()
	{
		UpdateExclamationMarks();
	}

	public void FocusCharacterPurchase(CharacterData character)
	{
		if (tabButtons == null) return;
		tabButtons.ButtonPressed(0, true);
		if (unlockContainers == null) return;
		foreach (var container in unlockContainers)
			if (container != null && container.unlockable == character) { ButtonManager.ForceHoverButton(container.GetComponent<MyButton>()); break; }
	}

	private void UpdateExclamationMarks()
	{
		var data = DataManager.Instance;
		var names = SaveManager.Instance?.progression?.newUnlockables;
		if (data == null || names == null) return;
		bool characters = false, weapons = false, tomes = false, items = false, hats = false;
		if (data.unsortedCharacterData != null) foreach (var entry in data.unsortedCharacterData) if (entry != null && names.Contains(entry.GetInternalName())) characters = true;
		if (data.unsortedWeapons != null) foreach (var entry in data.unsortedWeapons) if (entry != null && names.Contains(entry.GetInternalName())) weapons = true;
		if (data.unsortedTomes != null) foreach (var entry in data.unsortedTomes) if (entry != null && names.Contains(entry.GetInternalName())) tomes = true;
		if (data.unsortedItems != null) foreach (var entry in data.unsortedItems) if (entry != null && names.Contains(entry.GetInternalName())) items = true;
		if (data.unsortedHats != null) foreach (var entry in data.unsortedHats) if (entry != null && names.Contains(entry.GetInternalName())) hats = true;
		if (exclChar != null) exclChar.SetActive(characters);
		if (exclWeapon != null) exclWeapon.SetActive(weapons);
		if (exclTome != null) exclTome.SetActive(tomes);
		if (exclItem != null) exclItem.SetActive(items);
		if (exclHats != null) exclHats.SetActive(hats);
	}

	private void OnTabSelected(int index)
	{
		var data = DataManager.Instance;
		if (data == null) return;
		var items = new List<UnlockableBase>();
		switch (index)
		{
			case 0: if (data.unsortedCharacterData != null) items.AddRange(data.unsortedCharacterData); break;
			case 1: if (data.unsortedWeapons != null) items.AddRange(data.unsortedWeapons); break;
			case 2: if (data.unsortedTomes != null) items.AddRange(data.unsortedTomes); break;
			case 3: if (data.unsortedItems != null) items.AddRange(data.unsortedItems); break;
			case 4: if (data.unsortedHats != null) items.AddRange(data.unsortedHats); break;
			default: return;
		}
		Refresh(items);
	}

	private void Refresh(List<UnlockableBase> unlockables)
	{
		if (unlockContainerPrefab == null || unlockables == null) return;
		unlockContainers ??= new List<UnlockContainer>();
		unlockables.RemoveAll(item => item == null);
		unlockables.Sort();
		if (unlockContainers.Count == 0)
		{
			var first = unlockContainerPrefab.GetComponent<UnlockContainer>();
			if (first != null) unlockContainers.Add(first);
		}
		foreach (var card in unlockContainers) if (card != null) card.gameObject.SetActive(false);
		int index = 0;
		foreach (var item in unlockables)
		{
			if (!item.isEnabled || !item.showInUnlocks) continue;
			if (index >= unlockContainers.Count)
			{
				var card = Instantiate(unlockContainerPrefab, contentParent).GetComponent<UnlockContainer>();
				if (card == null) break;
				unlockContainers.Add(card);
			}
			unlockContainers[index].Set(item);
			unlockContainers[index++].gameObject.SetActive(true);
		}
		if (tabGridNavigation != null && tabButtons != null && tabButtons.current >= 0) tabGridNavigation.Set(tabButtons.GetSelectedButton());
	}
}
