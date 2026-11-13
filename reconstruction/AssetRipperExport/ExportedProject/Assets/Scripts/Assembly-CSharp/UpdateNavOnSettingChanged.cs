using Assets.Scripts.UI.Menu.Windows;
using UnityEngine;

public class UpdateNavOnSettingChanged : MonoBehaviour
{
	public TabsExplicitNavigation navigation;

	public AutoScrollRect autoScrollRect;

	private void Awake()
	{
		CurrentSettings.A_SettingUpdated += OnSettingUpdated;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
	}

	private void OnEnable()
	{
		Refresh();
	}

	private void OnSettingUpdated(string settingName, object o1, object o2)
	{
		if (settingName == "show_advanced_settings") Invoke(nameof(Refresh), 0f);
	}

	private void Refresh()
	{
		if (navigation != null) navigation.Refresh();
		if (autoScrollRect != null) autoScrollRect.ManualRefresh();
	}

	private void OnValidate()
	{
		navigation = GetComponent<TabsExplicitNavigation>();
	}
}
