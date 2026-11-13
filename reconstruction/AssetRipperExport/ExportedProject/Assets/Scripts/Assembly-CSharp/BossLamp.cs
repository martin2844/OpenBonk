using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class BossLamp : MonoBehaviour
{
	private float chargeTime = 3f;

	private float chargeProgress;

	public Renderer zoneRenderer;

	public Renderer lampRenderer;

	private MaterialPropertyBlock zonePropertyBlock;

	public Color zoneColor;

	private Color startColor;

	public Material lampPostMaterial;

	public Material lampPostMaterialOff;

	private float zoneRadius;

	private static int numPlayers;

	private bool hasPlayer;

	public GameObject minimapIcon;

	public GameObject altarIcon;

	public Image circleProgress;

	public CanvasGroup circleParent;

	public AudioSource audioStart;

	public AudioSource audioLoop;

	public AudioSource audioComplete;

	public AudioSource audioAbort;

	public GameObject light;

	public GameObject lampExplosionPrefab;

	public EffectPlayer finishedFx;

	public EffectPlayer randomGoOutEffect;

	private bool charging;

	private float pitchStart = 0.5f;

	private float pitchEnd = 1.5f;

	private bool isTurnedOn;

	public static Action A_Activate;

	public static Action A_Deactivate;

	private bool wasLoopAudioPlayingWhenPaused;

	private int randomDeactivateTimeMin = 20;

	private int randomDeactivateTimeMax = 75;

	private float timeToDeactivate;

	private void Awake()
	{
		MyTime.A_Pause += OnPause;
	}

	private void OnDestroy()
	{
		MyTime.A_Pause -= OnPause;
	}

	private void Start()
	{
		hasPlayer = false;
		minimapIcon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		circleParent.alpha = 0f;
		zonePropertyBlock = new MaterialPropertyBlock();
		zoneRenderer.GetPropertyBlock(zonePropertyBlock);
		startColor = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0f);
		zonePropertyBlock.SetColor("_MainColor", Color.clear);
		zoneRenderer.SetPropertyBlock(zonePropertyBlock);
		zoneRenderer.enabled = false;
		zoneRadius = zoneRenderer.GetComponent<Collider>().bounds.extents.x + 0.25f;
	}

	public bool HasPlayer()
	{
		return isTurnedOn && Vector3.Distance(zoneRenderer.transform.position, MyPlayer.Instance.transform.position) < zoneRadius;
	}

	private void Update()
	{
		if (chargeProgress >= 1f || (chargeProgress <= 0f && !charging)) return;
		chargeProgress += MyTime.deltaTime / (charging ? chargeTime : chargeTime * -0.25f);
		if (chargeProgress >= 1f)
		{
			chargeProgress = 1f;
			Complete();
		}
		else if (chargeProgress <= 0f) chargeProgress = 0f;
		audioLoop.pitch = Mathf.Lerp(pitchStart, pitchEnd, chargeProgress);
		circleProgress.fillAmount = chargeProgress;
		zonePropertyBlock.SetColor("_MainColor", Color.Lerp(startColor, zoneColor, chargeProgress));
		zoneRenderer.SetPropertyBlock(zonePropertyBlock);
		if (chargeProgress <= 0f) zoneRenderer.enabled = false;
	}

	private void OnPause(bool paused)
	{
		if (paused)
		{
			wasLoopAudioPlayingWhenPaused = audioLoop.isPlaying;
			audioLoop.Pause();
		}
		else if (wasLoopAudioPlayingWhenPaused) audioLoop.Play();
	}

	private void Complete()
	{
		light.SetActive(true);
		isTurnedOn = true;
		audioLoop.Stop();
		audioComplete.Play();
		circleParent.alpha = 0f;
		finishedFx.Play();
		minimapIcon.SetActive(false);
		altarIcon.SetActive(false);
		Instantiate(lampExplosionPrefab, transform.position, Quaternion.identity);
		ControllerShaker.Shake(0, 0.4f, 0.2f);
		ExtendRandomDeactivateTime();
		lampRenderer.sharedMaterial = lampPostMaterial;
		A_Activate?.Invoke();
	}

	public void Deactivate()
	{
		if (!isTurnedOn) return;
		isTurnedOn = false;
		charging = false;
		chargeProgress = 0f;
		light.SetActive(false);
		zoneRenderer.enabled = false;
		lampRenderer.sharedMaterial = lampPostMaterialOff;
		randomGoOutEffect.Play();
		minimapIcon.SetActive(true);
		A_Deactivate?.Invoke();
	}

	private void OnTriggerEnter()
	{
		if (isTurnedOn || charging) return;
		circleParent.alpha = 1f;
		zoneRenderer.enabled = true;
		charging = true;
		audioLoop.pitch = 1f;
		audioLoop.volume = 1f;
		audioStart.Play();
		audioLoop.Play();
	}

	private void FixedUpdate()
	{
		CheckRandomDeactivate();
	}

	private void CheckRandomDeactivate()
	{
		if (!isTurnedOn || HasPlayer() || GraveyardBossRoom.GetDarknessIntensityMultiplier() < 0.85f) return;
		timeToDeactivate -= MyTime.fixedDeltaTime;
		if (timeToDeactivate <= 0f) Deactivate();
	}

	private void ExtendRandomDeactivateTime()
	{
		timeToDeactivate = global::Utility.MyRandom.random.Next(randomDeactivateTimeMin, randomDeactivateTimeMax);
	}

	private void OnTriggerExit()
	{
		if (isTurnedOn) { ExtendRandomDeactivateTime(); return; }
		circleParent.alpha = 0f;
		charging = false;
		audioAbort.Play();
		audioLoop.Stop();
	}
}
