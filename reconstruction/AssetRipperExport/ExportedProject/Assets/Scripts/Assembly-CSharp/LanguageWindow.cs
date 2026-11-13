using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.UI.Localization;

public class LanguageWindow : Window
{
	public GameObject languageButtonPrefab;

	public SelectionGroupToggleSingle selectionGroup;

	private List<SelectionGroupToggleSingleButton> buttons;

	private List<int> localeIndices;

	private new void Awake()
	{
		base.Awake();
		if (selectionGroup != null) selectionGroup.A_ButtonSelected += OnLanguageSelected;
	}

	private new void Start()
	{
		FindAllButtonsInWindow();
		Assets.Scripts.UI.UiUtility.RebuildUi(transform);
	}

	private new void OnDestroy()
	{
		if (selectionGroup != null) selectionGroup.A_ButtonSelected -= OnLanguageSelected;
		base.OnDestroy();
	}

	private new void OnEnable()
	{
		Refresh();
		base.OnEnable();
	}

	private void Refresh()
	{
		if (languageButtonPrefab == null || selectionGroup == null) return;
		if (buttons == null)
		{
			buttons = new List<SelectionGroupToggleSingleButton>();
			var template = languageButtonPrefab.GetComponent<SelectionGroupToggleSingleButton>();
			if (template == null) return;
			buttons.Add(template);
		}
		var locales = LocalizationSettings.AvailableLocales?.Locales;
		if (locales == null) return;
		if (localeIndices == null) localeIndices = new List<int>();
		localeIndices.Clear();
		for (int i = 0; i < locales.Count; i++)
			if (locales[i] != null && !string.IsNullOrEmpty(locales[i].Identifier.Code)) localeIndices.Add(i);
		while (buttons.Count < localeIndices.Count)
		{
			var clone = Instantiate(languageButtonPrefab, languageButtonPrefab.transform.parent);
			buttons.Add(clone.GetComponent<SelectionGroupToggleSingleButton>());
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			var button = buttons[i];
			if (button == null) continue;
			button.gameObject.SetActive(i < localeIndices.Count);
			if (i >= localeIndices.Count) continue;
			var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
			if (text != null) text.text = LocalizationUtility.GetLanguageName(locales[localeIndices[i]]);
			var uiButton = button.GetButton();
			if (uiButton != null)
			{
				var navigation = uiButton.navigation;
				navigation.mode = Navigation.Mode.Explicit;
				navigation.selectOnUp = i > 0 ? buttons[i - 1].GetButton() : null;
				navigation.selectOnDown = i + 1 < localeIndices.Count ? buttons[i + 1].GetButton() : null;
				uiButton.navigation = navigation;
			}
		}
		selectionGroup.FindButtons();
		int selected = SaveManager.Instance != null && SaveManager.Instance.config != null ? SaveManager.Instance.config.cfGameSettings.language : 0;
		int selectedRow = localeIndices.IndexOf(selected);
		if (selectedRow >= 0) selectionGroup.ForceSelect(selectedRow);
		FindAllButtonsInWindow();
		startBtn = selectedRow >= 0 ? buttons[selectedRow] : null;
	}

	private void OnLanguageSelected(SelectionGroupToggleSingleButton btn)
	{
		if (buttons == null || localeIndices == null || SaveManager.Instance == null || SaveManager.Instance.config == null || CurrentSettings.Instance == null) return;
		int index = buttons.IndexOf(btn);
		if (index < 0 || index >= localeIndices.Count) return;
		SaveManager.Instance.config.hasSelectedLanguage = true;
		CurrentSettings.Instance.BetterUpdateCfSettings("language", localeIndices[index], SaveManager.Instance.config.cfGameSettings);
	}
}
