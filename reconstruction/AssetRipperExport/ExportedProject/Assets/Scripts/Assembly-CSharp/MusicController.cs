using System.Collections.Generic;
using Assets.Scripts.Audio.Music;
using UnityEngine;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.MapsAndStages;

public class MusicController : MonoBehaviour
{
	public static MusicController Instance;

	public AudioSource musicSource;

	public AudioLowPassFilter lowpassFilter;

	private float volumeMultiplier = 1f;

	private float desiredVolumeMultiplier = 1f;

	private float desiredVolume = 1f;

	private float desiredLowpass = 22000f;

	public MusicTrack menuMusicTrack;

	private MusicTrack currentTrack;

	private bool isPlayingIntro;

	private float introLength;

	private float defaultPitch = 1f;

	private float nextCheckTime;

	private float checkCooldown = .05f;

	private FinalFightController finalFightController;

	private int lowpassLow = 600;

	private float currentDangerPitch = 1f;

	private Dictionary<MusicPauseZone, float> zoneInfluences = new Dictionary<MusicPauseZone, float>();

	private float zoneMultiplier = 1f;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		GameManager.A_StageStarted += OnStageStarted;
		MainMenu.A_MenuOpened += OnMainMenu;
		TransitionUI.A_transitionStart += OnSceneTransitionStart;
		PlayerHealth.A_Died += OnPlayerDied;
		MyTime.A_Pause += OnPause;
		ChestWindowUi.A_Open += OnChestOpening;
		ChestWindowUi.A_Close += OnChestClosed;
	}

	private void OnDestroy()
	{
		if (Instance != this) return;
		GameManager.A_StageStarted -= OnStageStarted;
		MainMenu.A_MenuOpened -= OnMainMenu;
		TransitionUI.A_transitionStart -= OnSceneTransitionStart;
		PlayerHealth.A_Died -= OnPlayerDied;
		MyTime.A_Pause -= OnPause;
		ChestWindowUi.A_Open -= OnChestOpening;
		ChestWindowUi.A_Close -= OnChestClosed;
		Instance = null;
	}

	private void OnStageStarted()
	{
		desiredVolumeMultiplier = 0f;
		zoneInfluences.Clear(); zoneMultiplier = 1f;
		PlayStageMusic();
	}

	private void OnSceneTransitionStart()
	{
		zoneInfluences.Clear(); zoneMultiplier = 1f;
		desiredVolumeMultiplier = 0f;
	}

	public void StopMusic()
	{
		desiredVolumeMultiplier = 0f;
	}

	public void PlayStageMusic()
	{
		MusicTrack track = MusicUtility.GetMusicTrackToPlay(MapController.runConfig);
		if (track == null || musicSource == null) return;
		PlayMusicTrack(track);
		desiredVolume = 1f;
		musicSource.volume = 1f;
	}

	public void PlayMenuTrack()
	{
		if (menuMusicTrack == null || musicSource == null) return;
		zoneInfluences.Clear(); zoneMultiplier = 1f;
		PlayMusicTrack(menuMusicTrack);
		desiredVolume = .75f;
		musicSource.volume = .75f;
	}

	public void PlayMusicTrack(MusicTrack musicTrack)
	{
		if (musicSource == null || musicTrack == null) return;
		if (currentTrack != null && currentTrack != musicTrack) currentTrack.UnloadFromMemory();
		if (currentTrack == musicTrack && desiredVolumeMultiplier != 0f) return;
		currentTrack = musicTrack;
		musicTrack.LoadToMemory();
		if (musicTrack.intro != null)
		{
			musicSource.clip = musicTrack.intro;
			musicSource.loop = false;
			isPlayingIntro = true;
			introLength = musicTrack.intro.length;
		}
		else
		{
			musicSource.loop = true;
			isPlayingIntro = false;
			musicSource.clip = musicTrack.loop;
		}
		if (musicSource.clip != null) musicSource.Play();
		desiredVolumeMultiplier = 1f;
	}

	private void OnMainMenu()
	{
		currentDangerPitch = 1f;
		PlayMenuTrack();
	}

	private void Update()
	{
		if (musicSource == null) return;
		if (isPlayingIntro && (musicSource.time >= introLength - .02f || !musicSource.isPlaying))
		{
			musicSource.clip = currentTrack != null ? currentTrack.loop : null;
			musicSource.loop = true;
			if (musicSource.clip != null) musicSource.Play();
			isPlayingIntro = false;
		}
		UpdateZoneInfluences();
		volumeMultiplier = Mathf.Lerp(volumeMultiplier, desiredVolumeMultiplier, Time.deltaTime * 3f);
		musicSource.volume = volumeMultiplier * desiredVolume * zoneMultiplier;
		UpdateDesiredLowpass();
		if (lowpassFilter != null) lowpassFilter.cutoffFrequency = Mathf.Lerp(lowpassFilter.cutoffFrequency, desiredLowpass, Time.deltaTime * 8f);
		UpdatePitch();
	}

	private void UpdatePitch()
	{
		if (musicSource == null) return;
		var player = MyPlayer.Instance;
		var config = SaveManager.Instance != null ? SaveManager.Instance.config : null;
		if (player == null || config == null || config.cfVisualsSettings == null || config.cfVisualsSettings.low_hp_effects == 0 || player.playerEffects == null)
		{
			musicSource.pitch = defaultPitch;
			return;
		}
		float danger = player.playerEffects.GetDangerRatioMusic();
		if (danger > .01f)
			currentDangerPitch = Mathf.Lerp(currentDangerPitch, (1f - .25f * danger) * defaultPitch, Time.deltaTime * .5f);
		else if (currentDangerPitch < .99f)
			currentDangerPitch = Mathf.Lerp(currentDangerPitch, 1f, Time.deltaTime * 2f);
		else currentDangerPitch = 1f;
		musicSource.pitch = currentDangerPitch;
	}

	private void UpdateDesiredLowpass()
	{
		if (Time.time < nextCheckTime) return;
		nextCheckTime = Time.time + checkCooldown;
		desiredLowpass = MyTime.paused ? lowpassLow : 22000f;
		if (GameManager.Instance == null) return;
		if (MapController.currentMap != null && MapController.currentMap.eMap == EMap.Graveyard &&
			RsgController.Instance != null && RsgController.Instance.roomBoss != null &&
			RsgController.Instance.roomBoss.isBossDefeated && !GameManager.Instance.IsFinalSwarm())
			desiredLowpass = lowpassLow;
		if (MapController.IsFinalBossStage())
		{
			if (finalFightController == null) finalFightController = FindAnyObjectByType<FinalFightController>();
			else if (finalFightController.isBossDefeated && !GameManager.Instance.IsFinalSwarm()) desiredLowpass = lowpassLow;
		}
	}

	private void OnSettingUpdated(string name, object oldValue, object newValue)
	{
	}

	public void SetMusicVolume(float volume)
	{
		desiredVolume = volume;
		if (musicSource != null) musicSource.volume = volume;
	}

	public void RegisterZoneInfluence(MusicPauseZone zone, float influence)
	{
		if (zone != null) zoneInfluences[zone] = influence;
	}

	private void UpdateZoneInfluences()
	{
		zoneMultiplier = Mathf.Lerp(zoneMultiplier, 1f, Time.deltaTime);
		foreach (float influence in zoneInfluences.Values)
			if (influence < zoneMultiplier) zoneMultiplier = Mathf.Lerp(zoneMultiplier, influence, Time.deltaTime * 7f);
	}

	private void OnPlayerDied()
	{
		if (musicSource != null) musicSource.Stop();
	}

	private void OnChestOpening()
	{
		desiredVolumeMultiplier = .2f;
	}

	private void OnChestClosed()
	{
		desiredVolumeMultiplier = 1f;
	}

	private void OnPause(bool p)
	{
		UpdateDesiredLowpass();
	}

	public void RefreshFilter()
	{
		UpdateDesiredLowpass();
	}
}
