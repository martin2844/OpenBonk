using System;
using System.Collections;
using Assets.Scripts.Steam.LeaderboardsNew;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardUiNew : MonoBehaviour
{
	private const string BaseUrl = "https://openbonk-leaderboard.vercel.app";

	private const int MaxEntries = 25;

	public GameObject lbPrefab;

	private System.Collections.Generic.List<LeaderboardEntryUi> leaderboardEntries;

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
		UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
		RefreshFromServer();
#endif
	}

	private void OnDestroy()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
#endif
	}

	private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
	{
		RefreshFromServer();
	}

	private void RefreshFromServer()
	{
		StopAllCoroutines();
		StartCoroutine(Fetch());
	}

	private IEnumerator Fetch()
	{
		if (buffering != null) buffering.SetActive(true);
		string board = isWeekly ? "kills_weekly" : "kills";
		using (var request = UnityWebRequest.Get(BaseUrl + "/api/v1/leaderboard?board=" + board + "&limit=" + MaxEntries))
		{
			request.timeout = 10;
			yield return request.SendWebRequest();
			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogWarning("OpenBonk leaderboard read: " + request.error);
				ShowUnavailable();
				yield break;
			}
			LeaderboardResponse response = null;
			try { response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text); }
			catch (Exception e) { Debug.LogWarning("OpenBonk leaderboard parse: " + e.GetType().Name); }
			if (response == null || response.entries == null || response.entries.Length == 0)
			{
				ShowMessage(Localized("No scores yet", "Todavía no hay puntajes"));
				yield break;
			}
			ShowRows(response.entries);
		}
	}

	private LeaderboardResponse.Entry[] shownEntries;

	private int firstShown;

	private float dragRemainder;

	// UI masks do not clip in this port, so rows past the panel's height were
	// drawn below it. Only the rows that fit are shown, and dragging or the
	// mouse wheel changes which part of the top entries they display.
	private void ShowRows(LeaderboardResponse.Entry[] entries)
	{
		if (entries.Length > MaxEntries) Array.Resize(ref entries, MaxEntries);
		shownEntries = entries;
		firstShown = 0;
		dragRemainder = 0f;
		RenderRows();
		if (buffering != null) buffering.SetActive(false);
		if (t_reset != null) t_reset.text = "";
	}

	private void RenderRows()
	{
		var entries = shownEntries;
		if (entries == null) return;
		var template = GetComponentsInChildren<LeaderboardEntryUi>(true);
		if (template.Length == 0) return;
		var container = template[0].transform.parent as RectTransform;
		SetUpDrag(container);
		int fit = Mathf.Min(entries.Length, RowsThatFit(container, template[0]));
		firstShown = Mathf.Clamp(firstShown, 0, Mathf.Max(0, entries.Length - fit));
		var rows = EnsureRows(fit);
		for (int i = 0; i < rows.Length; i++) rows[i].gameObject.SetActive(false);
		int count = Mathf.Min(fit, rows.Length);
		for (int i = 0; i < count; i++)
		{
			var row = entries[firstShown + i];
			var ui = rows[i];
			ui.gameObject.SetActive(true);
			if (ui.playerName != null) ui.playerName.text = string.IsNullOrEmpty(row.name) ? "Player" : row.name;
			if (ui.rank != null) ui.rank.text = row.rank.ToString();
			if (ui.score != null) ui.score.text = row.score.ToString("N0");
			if (ui.playerIcon != null) ui.playerIcon.enabled = false;
			if (ui.characterIcon != null)
			{
				var icon = CharacterIcon(row.character);
				ui.characterIcon.texture = icon;
				ui.characterIcon.enabled = icon != null;
			}
			if (ui.localHighlight != null) ui.localHighlight.enabled = false;
			if (ui.hoverOverlay != null) ui.hoverOverlay.SetActive(false);
		}
	}

	private int RowsThatFit(RectTransform container, LeaderboardEntryUi sample)
	{
		const int fallback = 10;
		var viewport = container != null ? container.parent as RectTransform : null;
		var row = sample.transform as RectTransform;
		if (viewport == null || row == null) return fallback;
		float available = viewport.rect.height;
		float rowHeight = row.rect.height > 0f ? row.rect.height : LayoutUtility.GetPreferredHeight(row);
		float spacing = 0f;
		var layout = container.GetComponent<VerticalLayoutGroup>();
		if (layout != null)
		{
			spacing = layout.spacing;
			available -= layout.padding.top + layout.padding.bottom;
		}
		if (available <= 0f || rowHeight <= 0f) return fallback;
		return Mathf.Clamp(Mathf.FloorToInt((available + spacing) / (rowHeight + spacing)), 1, MaxEntries);
	}

	private float RowStep(RectTransform container)
	{
		var rows = GetComponentsInChildren<LeaderboardEntryUi>(false);
		float height = rows.Length > 0 ? ((RectTransform)rows[0].transform).rect.height : 0f;
		var layout = container != null ? container.GetComponent<VerticalLayoutGroup>() : null;
		float step = height + (layout != null ? layout.spacing : 0f);
		return step > 1f ? step : 40f;
	}

	private void SetUpDrag(RectTransform container)
	{
		if (container == null || container.GetComponent<OpenBonkLeaderboardDrag>() != null) return;
		// The panel's own ScrollRect would move the rows out of the panel.
		var viewport = container.parent;
		if (viewport != null)
			foreach (var behaviour in viewport.GetComponents<Behaviour>())
				if (behaviour is ScrollRect || behaviour.GetType().Name == "AutoScrollRect") behaviour.enabled = false;
		var drag = container.gameObject.AddComponent<OpenBonkLeaderboardDrag>();
		drag.moved = distance => Scroll(container, distance);
	}

	// distance is in canvas units; positive moves towards lower ranks.
	private void Scroll(RectTransform container, float distance)
	{
		if (shownEntries == null) return;
		dragRemainder += distance;
		float step = RowStep(container);
		int rows = (int)(dragRemainder / step);
		if (rows == 0) return;
		dragRemainder -= rows * step;
		int before = firstShown;
		firstShown += rows;
		RenderRows();
		if (firstShown == before) dragRemainder = 0f;
	}

	// The menu panel ships with a single row, so further rows are created from
	// lbPrefab (or a copy of the first row) next to it.
	private LeaderboardEntryUi[] EnsureRows(int wanted)
	{
		var rows = GetComponentsInChildren<LeaderboardEntryUi>(true);
		if (rows.Length == 0 || rows.Length >= wanted) return rows;
		var parent = rows[0].transform.parent;
		var template = lbPrefab != null && lbPrefab.GetComponent<LeaderboardEntryUi>() != null ? lbPrefab : rows[0].gameObject;
		for (int i = rows.Length; i < wanted; i++)
		{
			var row = Instantiate(template, parent, false);
			row.name = template.name + " " + i;
			row.SetActive(false);
		}
		return GetComponentsInChildren<LeaderboardEntryUi>(true);
	}

	private void ShowUnavailable()
	{
		ShowMessage(Localized("Leaderboard unavailable", "Clasificación no disponible"));
	}

	private void ShowMessage(string text)
	{
		shownEntries = null;
		var rows = GetComponentsInChildren<LeaderboardEntryUi>(true);
		LeaderboardEntryUi message = rows.Length > 0 ? rows[0] : null;
		for (int i = 0; i < rows.Length; i++) rows[i].gameObject.SetActive(false);
		if (message != null)
		{
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
				message.playerName.text = text;
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

	// Vanilla characters are sent by enum name and mod characters as
	// "package:id". Mod portraits come from the loaded mod when it is enabled,
	// otherwise from the copies bundled in OpenBonkBundledCharacterIcons.
	private static Texture CharacterIcon(string character)
	{
		if (string.IsNullOrEmpty(character) || character.Length > 64) return null;
		int separator = character.IndexOf(':');
		try
		{
			var data = DataManager.Instance;
			if (data != null)
			{
				CharacterData found;
				if (separator > 0)
				{
					var id = (ECharacter)OpenBonkContentIds.Character(character.Substring(0, separator), character.Substring(separator + 1));
					if (data.TryGetCharacterData(id, out found) && found != null && found.icon != null) return found.icon;
				}
				else if (Enum.TryParse(character, false, out ECharacter vanilla) && Enum.IsDefined(typeof(ECharacter), vanilla) &&
					data.TryGetCharacterData(vanilla, out found) && found != null && found.icon != null)
					return found.icon;
			}
		}
		catch (Exception e)
		{
			Debug.LogWarning("OpenBonk leaderboard icon: " + e.GetType().Name);
		}
		return separator > 0 ? OpenBonkBundledCharacterIcons.Get(character) : null;
	}

	private static string Localized(string english, string spanish)
	{
		var locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
		string code = locale != null ? locale.Identifier.Code : "en";
		return code.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? spanish : english;
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

	[Serializable]
	private sealed class LeaderboardResponse
	{
		public string board;
		public string period;
		public Entry[] entries;

		[Serializable]
		public sealed class Entry
		{
			public int rank;
			public string name;
			public int score;
			public string version;
			public string character;
			public string map;
			public int duration;
			public bool mods;
			public string at;
		}
	}
}
