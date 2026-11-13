using System;
using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;
using UnityEngine.UI;

public class SkinSelection : MonoBehaviour
{
	public GameObject skinContainerPrefab;

	private List<SkinContainer> containers;

	public MyButton b_confirm;

	private List<SkinData> skins;

	private int currentlySelected;

	private SkinContainer previousSelected;

	public static Action<SkinContainer> A_ForceSkinDisplay;

	private SkinContainer lastSelectedSkinContainer;

	private void Awake()
	{
		SkinContainer.A_Hover += OnSkinHover;
		SkinContainer.A_HoverMouse += OnSkinHoverMouse;
		SkinContainer.A_HoverMouseExit += OnSkinHoverMouseExit;
	}

	private void OnDestroy()
	{
		SkinContainer.A_Hover -= OnSkinHover;
		SkinContainer.A_HoverMouse -= OnSkinHoverMouse;
		SkinContainer.A_HoverMouseExit -= OnSkinHoverMouseExit;
	}

	private void Start()
	{
	}

	private void OnSkinHover(SkinContainer skinContainer)
	{
		lastSelectedSkinContainer = skinContainer;
		int index = containers.IndexOf(skinContainer);
		if (index >= 0) SetCurrentlySelected(index, skinContainer.GetCharacter());
		SetConfirmButtonNav();
	}

	private void OnSkinHoverMouse(SkinContainer skinContainer)
	{
	}

	private void OnSkinHoverMouseExit(SkinContainer skinContainer)
	{
		OnSkinHover(lastSelectedSkinContainer);
	}

	private void SetCurrentlySelected(int index, ECharacter character)
	{
		currentlySelected = index;
		if (MyAchievements.IsPurchased(containers[index].skin))
			SaveManager.Instance.config.preferences.characterSkins[character] = index;
		if (previousSelected != null) previousSelected.SetSelected(false);
		containers[index].SetSelected(true);
		previousSelected = containers[index];
		lastSelectedSkinContainer = containers[index];
		A_ForceSkinDisplay?.Invoke(lastSelectedSkinContainer);
	}

	public void DisableNavigation()
	{
	}

	public void CreateNavigation(Button backButton)
	{
		for (int i = 0; i < skins.Count; i++)
		{
			var navigation = new Navigation { mode = Navigation.Mode.Explicit };
			navigation.selectOnLeft = i == 0 ? backButton : containers[i - 1].GetButton();
			navigation.selectOnRight = i < containers.Count - 1 ? containers[i + 1].GetButton() : null;
			navigation.selectOnDown = b_confirm.GetButton();
			containers[i].GetButton().navigation = navigation;
		}
		SetConfirmButtonNav();
	}

	public void EnableNavigation(Button backButton)
	{
		CreateNavigation(backButton);
	}

	private void SetConfirmButtonNav()
	{
		var button = b_confirm.GetButton();
		var navigation = button.navigation;
		navigation.mode = Navigation.Mode.Explicit;
		navigation.selectOnUp = containers[currentlySelected].GetButton();
		button.navigation = navigation;
	}

	public void SetCharacter(MyButtonCharacter charButton)
	{
		ECharacter character = charButton.characterData.eCharacter;
		skins = DataManager.Instance.GetSkins(character);
		skins.Sort((a, b) => a.CompareTo(b));
		if (containers == null) containers = new List<SkinContainer> { skinContainerPrefab.GetComponent<SkinContainer>() };
		while (containers.Count < skins.Count)
			containers.Add(Instantiate(skinContainerPrefab, skinContainerPrefab.transform.parent).GetComponent<SkinContainer>());
		for (int i = 0; i < containers.Count; i++)
		{
			containers[i].gameObject.SetActive(i < skins.Count);
			if (i >= skins.Count) continue;
			containers[i].SetSelected(false);
			containers[i].SetSkin(skins[i]);
		}
		int selected = SaveManager.Instance.config.preferences.GetCharacterSkin(character);
		if (selected < 0 || selected >= skins.Count) selected = 0;
		if (!MyAchievements.IsPurchased(containers[selected].skin)) selected = 0;
		SetCurrentlySelected(selected, character);
		CreateNavigation(charButton.GetButton());
	}

	public void SetNotUnlocked()
	{
		if (containers != null) foreach (var container in containers) container.gameObject.SetActive(false);
	}
}
