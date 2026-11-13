using System.Collections.Generic;
using Assets.Scripts.Steam.LeaderboardsNew;
using TMPro;
using UnityEngine;

public class LeaderboardUiNew : MonoBehaviour
{
	public GameObject lbPrefab;

	private List<LeaderboardEntryUi> leaderboardEntries;

	public GameObject buffering;

	public ButtonNavigationSelectionOnly leaderboardTypeButtons;

	private static int lastSelectedTypeIndex;

	public TextMeshProUGUI t_reset;

	private SteamLeaderboardNew leaderboard;

	private int numEntriesToShow;

	private bool isWeekly;

	private bool isGlobal;

	private void Awake()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		ShowUnavailable();
		UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
#endif
	}

	// The Steam backend is not present on Android. Do not display the serialized
	// editor samples as real scores while the backend is unavailable.
	private void ShowUnavailable()
	{
		var entries = GetComponentsInChildren<LeaderboardEntryUi>(true);
		LeaderboardEntryUi message = null;
		foreach (var entry in entries)
		{
			if (message == null && entry.gameObject.activeSelf) message = entry;
			entry.gameObject.SetActive(false);
		}
		if (message != null)
		{
			var locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
			string code = locale != null ? locale.Identifier.Code : "en";
			if (message.playerName != null)
			{
				var rect = message.playerName.rectTransform;
				rect.SetParent(message.transform, false);
				rect.anchorMin = Vector2.zero;
				rect.anchorMax = Vector2.one;
				rect.offsetMin = new Vector2(8, 0);
				rect.offsetMax = new Vector2(-8, 0);
				message.playerName.alignment = TextAlignmentOptions.Center;
				message.playerName.enableAutoSizing = true;
				message.playerName.fontSizeMin = 10;
				message.playerName.text = code.StartsWith("es", System.StringComparison.OrdinalIgnoreCase)
					? "Clasificación no disponible" : "Leaderboard unavailable";
			}
			if (message.rank != null) message.rank.text = "";
			if (message.score != null) message.score.text = "";
			if (message.playerIcon != null) message.playerIcon.enabled = false;
			if (message.characterIcon != null) message.characterIcon.enabled = false;
			if (message.localHighlight != null) message.localHighlight.enabled = false;
			if (message.hoverOverlay != null) message.hoverOverlay.SetActive(false);
			message.gameObject.SetActive(true);
		}
		if (buffering != null) buffering.SetActive(false);
		if (t_reset != null) t_reset.text = "";
	}

	private void OnDestroy()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
#endif
	}

	private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
	{
		ShowUnavailable();
	}

	private void TryInit()
	{
	}

	private void Start()
	{
	}

	private void OnLeaderboardReady(SteamLeaderboardNew leaderboardReady)
	{
	}

	private void Refresh()
	{
	}

	private void OnLeaderboardTypeSelected(int index)
	{
	}
}
