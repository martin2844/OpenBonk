using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;

public class AlertUi : MonoBehaviour
{
	public TextMeshProUGUI t_alert;

	public AudioSource audio;

	public AudioClip c_swarm;

	public AudioClip c_finalSwarm;

	public AudioClip c_boss;

	public Color swarmColor;

	public Color swarmFinalColor;

	public Color bossColor;

	private float timer;

	private bool startedFade;

	private void Awake()
	{
		SummonerController.A_SwarmStarted += OnSwarmStarted;
		SummonerController.A_FinalSwarmStarted += OnFinalSwarmStarted;
		InteractableBossSpawner.A_BossSpawned += SetAlertBoss;
	}

	private void OnDestroy()
	{
		SummonerController.A_SwarmStarted -= OnSwarmStarted;
		SummonerController.A_FinalSwarmStarted -= OnFinalSwarmStarted;
		InteractableBossSpawner.A_BossSpawned -= SetAlertBoss;
	}

	private void OnSwarmStarted()
	{
		SetAlert(EWaveType.Swarm);
	}

	private void OnFinalSwarmStarted()
	{
		SetAlert(EWaveType.FinalSwarm);
	}

	private void OnNewWave(EWaveType waveType)
	{
		if (waveType == EWaveType.Swarm || waveType == EWaveType.FinalSwarm) SetAlert(waveType);
	}

	public void SetAlertBoss()
	{
		ShowAlert("ALERT_BOSS", bossColor, c_boss);
	}

	public void SetAlertTimesUp()
	{
		ShowAlert("ALERT_TIME_UP", bossColor, c_finalSwarm);
	}

	public void SetAlert(EWaveType waveType)
	{
		if (waveType == EWaveType.Swarm) ShowAlert("ALERT_SWARM", swarmColor, c_swarm);
		else if (waveType == EWaveType.FinalSwarm) ShowAlert("ALERT_FINAL_SWARM", swarmFinalColor, c_finalSwarm);
		else AnimateAlert();
	}

	private void ShowAlert(string key, Color color, AudioClip clip)
	{
		if (t_alert == null) return;
		t_alert.fontSize = 62f;
		t_alert.color = color;
		if (audio != null) audio.clip = clip;
		t_alert.text = LocalizationUtility.GetLocalizedString("Game_HUD", key);
		AnimateAlert();
	}

	private void AnimateAlert()
	{
		if (t_alert == null) return;
		CancelInvoke();
		if (audio != null) audio.Play();
		t_alert.gameObject.SetActive(true);
		t_alert.transform.localScale = Vector3.zero;
		if (audio != null) audio.Play();
		t_alert.CrossFadeAlpha(1f, 0f, true);
		startedFade = false;
		timer = 0f;
	}

	private void Update()
	{
		if (MyTime.paused || t_alert == null || !t_alert.gameObject.activeInHierarchy) return;
		t_alert.alpha = Mathf.PingPong(Time.time * 4f, 1f) + 0.25f;
		t_alert.transform.localScale = Vector3.Lerp(t_alert.transform.localScale, Vector3.one, Time.deltaTime * 8f);
		timer += MyTime.deltaTime;
		if (timer >= 2.5f && !startedFade) StartFade();
	}

	private void StartFade()
	{
		startedFade = true;
		if (t_alert != null) t_alert.CrossFadeAlpha(0f, 1f, true);
		Invoke(nameof(Disable), 1f);
	}

	private void Disable()
	{
		if (t_alert != null) t_alert.gameObject.SetActive(false);
	}
}
