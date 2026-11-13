using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;

public class ChargeShrine : BaseInteractable
{
	private float chargeTime = 5f;

	private float currentChargeTime = 5f;

	private float chargeProgress;

	public Renderer zoneRenderer;

	public Renderer meshRenderer;

	public Transform runeStone;

	private MaterialPropertyBlock zonePropertyBlock;

	private MaterialPropertyBlock rendererPropertyBlock;

	private Color zoneColor;

	private Color startColor;

	public GameObject minimapIcon;

	public Image circleProgress;

	public CanvasGroup circleParent;

	public AudioSource audioStart;

	public AudioSource audioLoop;

	public AudioSource audioComplete;

	public AudioSource audioAbort;

	public GameObject finishParticles;

	public GameObject altarIcon;

	public GameObject healingZone;

	private bool notInterrupted = true;

	public static Action A_ChargeShrineSpawned;

	public static Action<bool> A_Charged;

	private float goldChance = .008f;

	public Material goldMaterial;

	private bool wasLoopAudioPlayingWhenPaused;

	private float pitchStart = .5f;

	private float pitchEnd = 1.5f;

	private bool completed;

	private float rewardTime;

	private bool rewardGiven;

	public static ChargeShrine lastRewardShrine;

	private bool charging;

	private float volumeWhenExit;

	public static string debugName = "Charge Shrines";

	public bool isGolden { get; private set; }

	private void Awake()
	{
		isGolden = UnityEngine.Random.Range(0f, 1f) < goldChance;
		if (isGolden) meshRenderer.material = goldMaterial;
		MyTime.A_Pause += OnPause;
	}

	private new void OnDestroy()
	{
		MyTime.A_Pause -= OnPause;
	}

	private new void Start()
	{
		base.Start();
		minimapIcon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		circleParent.alpha = 0f;
		runeStone.localScale = Vector3.zero;
		runeStone.gameObject.SetActive(false);
		zonePropertyBlock = new MaterialPropertyBlock();
		zoneRenderer.GetPropertyBlock(zonePropertyBlock);
		zoneColor = UnityEngine.Random.ColorHSV(0f, 1f, .5f, 1f, .5f, 1f, 1f, 1f);
		startColor = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0f);
		rendererPropertyBlock = new MaterialPropertyBlock();
		meshRenderer.GetPropertyBlock(rendererPropertyBlock);
		rendererPropertyBlock.SetColor("_EmissionColor", zoneColor * 0f);
		meshRenderer.SetPropertyBlock(rendererPropertyBlock);
		zonePropertyBlock.SetColor("_MainColor", Color.clear);
		zoneRenderer.SetPropertyBlock(zonePropertyBlock);
		zoneRenderer.enabled = false;
		A_ChargeShrineSpawned?.Invoke();
	}

	private void FindChargeTime()
	{
		var wrench = MyPlayer.Instance.inventory.itemInventory.GetItem(EItem.Wrench) as ItemWrench;
		currentChargeTime = wrench == null ? chargeTime : Mathf.Clamp(chargeTime * (1f - wrench.GetChargeSpeedIncrease()), .2f, chargeTime);
	}

	private void Update()
	{
		if (!rewardGiven && completed && MyTime.time >= rewardTime) Reward();
		if (chargeProgress < 1f && (chargeProgress > 0f || charging))
		{
			chargeProgress += MyTime.deltaTime / (charging ? currentChargeTime : currentChargeTime * -.25f);
			if (chargeProgress >= 1f) { chargeProgress = 1f; Complete(); }
			else if (chargeProgress <= 0f) chargeProgress = 0f;
			audioLoop.pitch = Mathf.Lerp(pitchStart, pitchEnd, chargeProgress);
			circleProgress.fillAmount = chargeProgress;
			zonePropertyBlock.SetColor("_MainColor", Color.Lerp(startColor, zoneColor, chargeProgress));
			zoneRenderer.SetPropertyBlock(zonePropertyBlock);
			meshRenderer.GetPropertyBlock(rendererPropertyBlock);
			rendererPropertyBlock.SetColor("_EmissionColor", zoneColor * (3f * chargeProgress));
			meshRenderer.SetPropertyBlock(rendererPropertyBlock);
			runeStone.localScale = Vector3.one * chargeProgress;
		}
		if (chargeProgress <= 0f)
		{
			chargeProgress = 0f;
			zoneRenderer.enabled = false;
			runeStone.localScale = Vector3.zero;
		}
	}

	private void OnPause(bool paused)
	{
		if (paused) { wasLoopAudioPlayingWhenPaused = audioLoop.isPlaying; audioLoop.Pause(); }
		else if (wasLoopAudioPlayingWhenPaused) audioLoop.Play();
	}

	private void Complete()
	{
		completed = true;
		audioLoop.Stop();
		audioComplete.Play();
		zoneRenderer.enabled = false;
		circleParent.alpha = 0f;
		var fx = Instantiate(finishParticles, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
		var main = fx.main; main.startColor = zoneColor;
		Invoke(nameof(ScorePopup), .2f);
		rewardTime = MyTime.time + .5f;
		minimapIcon.SetActive(false);
		altarIcon.SetActive(false);
		ControllerShaker.Shake(0, .4f, .2f);
		if (MyPlayer.Instance.inventory.itemInventory.GetItem(EItem.Beacon) is ItemBeacon)
		{
			healingZone.transform.SetParent(null);
			healingZone.SetActive(true);
		}
		A_Charged?.Invoke(notInterrupted);
	}

	private void ScorePopup()
	{
		UiManager.Instance.scoreUi.AddScore(LocalizationUtility.GetLocalizedString("Game_ScoreUi", "POPUP_SHRINE"), "+1", true, true, 1f);
	}

	private void Reward()
	{
		if (rewardGiven) return;
		lastRewardShrine = this;
		rewardGiven = true;
		UiManager.Instance.encounterWindows.AddEncounter(EEncounter.RandomStats);
	}

	private void OnTriggerEnter()
	{
		if (completed || charging) return;
		circleParent.alpha = 1f;
		zoneRenderer.enabled = true;
		charging = true;
		audioLoop.pitch = 1f;
		audioLoop.volume = 1f;
		audioStart.Play();
		audioLoop.Play();
		runeStone.gameObject.SetActive(true);
		FindChargeTime();
	}

	private void OnTriggerExit()
	{
		if (completed) return;
		circleParent.alpha = 0f;
		charging = false;
		audioAbort.Play();
		audioLoop.Stop();
		notInterrupted = false;
	}

	public override bool Interact()
	{
		return false;
	}

	public override string GetInteractString()
	{
		return "";
	}

	public override bool CanInteract()
	{
		return false;
	}

	public override bool ShowInDebug()
	{
		return true;
	}

	public override string GetDebugName()
	{
		return debugName;
	}
}
