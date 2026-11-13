using System.Collections.Generic;
using Assets.Scripts.Audio.Music;
using TMPro;
using UnityEngine;

public class MusicSelectionUi : MonoBehaviour
{
	private static int index = -1;

	public TextMeshProUGUI t_trackName;

	private List<MusicTrack> tracks;

	public MapSelectionUi mapSelection;

	private void Awake()
	{
		MapSelectionUi.A_MapSelected += OnMapSelected;
	}

	private void OnDestroy()
	{
		MapSelectionUi.A_MapSelected -= OnMapSelected;
	}

	private void Start()
	{
		UpdateTrack(false);
	}

	public void Flip(int dir)
	{
		int next = index + dir;
		if (next < -1 || next >= NumSongs()) return;
		index = next;
		UpdateTrack(true);
	}

	private void UpdateTrack(bool playTrack)
	{
		var available = MusicUtility.GetTracks();
		// Available tracks can change while loading; keep the random selection valid.
		if (index >= available.Count) index = -1;
		MusicTrack selected = index >= 0 ? available[index] : null;
		if (t_trackName != null) t_trackName.text = index < 0 ? "Random" : selected != null ? selected.trackName ?? string.Empty : string.Empty;
		if (MusicController.Instance != null)
		{
			if (playTrack && selected != null) MusicController.Instance.PlayMusicTrack(selected);
			else MusicController.Instance.PlayMenuTrack();
		}
		if (mapSelection != null && mapSelection.runConfig != null) mapSelection.runConfig.musicTrackIndex = index;
	}

	private void OnMapSelected(SelectionGroupToggleSingleButton btn, MapData mapData)
	{
		UpdateTrack(false);
	}

	private int NumSongs()
	{
		return MusicUtility.GetTracks().Count;
	}
}
