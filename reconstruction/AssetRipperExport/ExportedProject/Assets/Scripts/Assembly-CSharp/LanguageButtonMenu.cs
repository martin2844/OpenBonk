using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Assets.Scripts.UI.Localization;

public class LanguageButtonMenu : MonoBehaviour
{
	public TextMeshProUGUI languageText;

	public TextSizer textSizer;

	public ButtonTextWrapper buttonTextWrapper;

	private void Start()
	{
		OnLocaleChanged(LocalizationSettings.SelectedLocale);
	}

	private void Awake()
	{
		LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
	}

	private void OnDestroy()
	{
		LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
	}

	private void OnLocaleChanged(Locale newLocale)
	{
		if (languageText != null) languageText.text = LocalizationUtility.GetLanguageName(newLocale);
		if (isActiveAndEnabled) Invoke(nameof(DelayedRefresh), 0f);
	}

	private void DelayedRefresh()
	{
		if (textSizer != null) { textSizer.Refresh(); textSizer.Recalculate(); }
		if (buttonTextWrapper != null) buttonTextWrapper.Refresh();
	}
}
