using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class MapEntry : MonoBehaviour
{
	public RawImage mapIcon;

	public GameObject locked;

	public GameObject notificationIcon;

	public SelectionGroupToggleSingleButton buttonScript;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_name;

	public MapData mapData { get; private set; }

	private void Awake()
	{
		MapSelectionUi.A_MapSelected += OnMapSelected;
		LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
	}

	private void OnDestroy()
	{
		MapSelectionUi.A_MapSelected -= OnMapSelected;
		LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
	}

	private void OnEnable()
	{
		// Entries may be initialized while their parent window is inactive, before
		// Awake can subscribe to a subsequent language change.
		if (mapData != null) Set(mapData);
	}

	private void OnLocaleChanged(Locale obj)
	{
		if (mapData != null) Set(mapData);
	}

	private void OnMapSelected(SelectionGroupToggleSingleButton arg1, MapData arg2)
	{
		if (mapData == null || arg2 == null || mapData.eMap != arg2.eMap || notificationIcon == null || !notificationIcon.activeSelf) return;
		SaveManager.Instance?.progression?.newMaps?.Remove(mapData.GetInternalName());
		notificationIcon.SetActive(false);
	}

	public void Set(MapData mapData)
	{
		this.mapData = mapData;
		if (mapData == null) return;
		bool available = mapData.achievementRequirement == null || Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(mapData.achievementRequirement);
		if (mapIcon != null) mapIcon.texture = mapData.icon;
		if (t_name != null) t_name.text = available ? mapData.GetName() : "??";
		if (t_description != null) t_description.text = available ? mapData.GetDescription() :
			Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Other", "REQUIRES", "Requires") +
			":<size=85%>\n" + mapData.achievementRequirement.GetUnlockRequirement();
		if (locked != null) locked.SetActive(!available);
		if (notificationIcon != null && available)
			notificationIcon.SetActive(SaveManager.Instance != null && SaveManager.Instance.progression != null && SaveManager.Instance.progression.newMaps != null && SaveManager.Instance.progression.newMaps.Contains(mapData.GetInternalName()));
		if (buttonScript != null) buttonScript.CanSelect(available);
	}
}
