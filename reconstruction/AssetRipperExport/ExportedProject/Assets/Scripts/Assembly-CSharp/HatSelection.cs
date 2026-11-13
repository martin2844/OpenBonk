using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using Assets.Scripts._Data.Hats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class HatSelection : MonoBehaviour
{
	public HatSelectionPopupWindow hatSelectionPopupWindow;

	public Texture noHatTexture;

	public LocalizedString localizedNoHat;

	public ButtonTextWrapper textWrapper;

	public TextSizer textSizer;

	private static int index;

	public TextMeshProUGUI t_hatName;

	public RawImage i_hatIcon;

	private List<HatData> availableHats;

	private ECharacter character;

	public HatData selectedHatData;

	public static Action A_HatChanged;

	public static Action<HatData> A_HatHover;

	private void Awake()
	{
		MyButtonCharacter.A_Select += OnSelectCharacter;
	}

	private void OnDestroy()
	{
		MyButtonCharacter.A_Select -= OnSelectCharacter;
	}

	private void OnEnable()
	{
		CheckInit(true);
		hatSelectionPopupWindow.RefreshHatButtons(availableHats, selectedHatData);
	}

	private void CheckInit(bool force)
	{
		if (availableHats != null && !force) return;
		availableHats = new List<HatData> { null };
		foreach (HatData hat in DataManager.Instance.unsortedHats)
			if (MyAchievements.IsPurchased(hat)) availableHats.Add(hat);
		availableHats.Sort();
	}

	public void HoverHat(HatData hatData)
	{
		A_HatHover?.Invoke(hatData);
	}

	public void SelectHat(HatData hatData)
	{
		CheckInit(false);
		selectedHatData = hatData;
		SaveManager.Instance.config.preferences.SetCharacterHat(character, hatData != null ? hatData.eHat : EHat.None);
		hatSelectionPopupWindow.FindStartButton(hatData);
		UpdateHatText();
		A_HatChanged?.Invoke();
	}

	private void UpdateHatText()
	{
		CheckInit(false);
		t_hatName.text = (selectedHatData != null ? selectedHatData.GetName() : localizedNoHat.GetLocalizedString()) ?? string.Empty;
		i_hatIcon.texture = selectedHatData != null ? selectedHatData.icon : noHatTexture;
		textSizer.Recalculate();
		textSizer.Refresh();
		textWrapper.Refresh();
	}

	private void OnSelectCharacter(MyButtonCharacter characterButton)
	{
		CheckInit(false);
		character = characterButton.characterData.eCharacter;
		selectedHatData = DataManager.Instance.GetHat(SaveManager.Instance.config.preferences.GetCharacterHat(character));
		hatSelectionPopupWindow.FindStartButton(selectedHatData);
		UpdateHatText();
	}

	private int NumSongs()
	{
		return availableHats.Count;
	}
}
