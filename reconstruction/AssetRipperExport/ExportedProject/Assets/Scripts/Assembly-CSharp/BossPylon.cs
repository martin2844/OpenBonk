using System;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class BossPylon : MonoBehaviour
{
	private Vector3 startPosition;

	private float chargeTime = 5f;

	private float chargeProgress;

	private MaterialPropertyBlock zonePropertyBlock;

	public Renderer zoneRenderer;

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

	public static Action<BossPylon> A_Charged;

	private Enemy boss;

	public LineRenderer laser;

	private int arcPointCount = 10;

	private float arcHeight = 15f;

	private float moveTime;

	private Vector3 fromPos;

	private Vector3 toPos;

	public ParticleSystem moveFx;

	private float height = 14f;

	private float moveOverSeconds = 2f;

	private bool burying;

	private float currentChargeTime;

	private bool wasLoopAudioPlayingWhenPaused;

	private float pitchStart = 0.5f;

	private float pitchEnd = 1.5f;

	private bool completed;

	private bool charging;

	private float volumeWhenExit;

	public void Set(Enemy enemy)
	{
		gameObject.SetActive(true);
		if (startPosition == Vector3.zero) startPosition = transform.position;
		moveFx.transform.SetParent(null);
		moveFx.transform.position = startPosition;
		boss = enemy;
		completed = false;
		laser.gameObject.SetActive(true);
		minimapIcon.SetActive(true);
		altarIcon.SetActive(true);
		burying = false;
		Reset();
		Surface();
	}

	private void Update()
	{
		ChargeUpdate();
		MoveUpdate();
		DrawLaser();
	}

	private void DrawLaser()
	{
		if (boss == null || !laser.gameObject.activeInHierarchy) return;
		Vector3 from = laser.transform.position;
		Vector3 to = boss.GetCenterPosition();
		laser.positionCount = arcPointCount;
		for (int i = 0; i < arcPointCount; i++)
		{
			float t = (float)i / (arcPointCount - 1);
			laser.SetPosition(i, Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight));
		}
	}

	private void Surface()
	{
		moveTime = 0f;
		transform.position = startPosition + Vector3.down * height;
		fromPos = transform.position;
		toPos = startPosition;
		moveFx.gameObject.SetActive(true);
		moveFx.Play();
	}

	private void Bury()
	{
		moveTime = 0f;
		fromPos = startPosition;
		toPos = startPosition + Vector3.down * height;
		moveFx.gameObject.SetActive(true);
		moveFx.Play();
		burying = true;
	}

	private void MoveUpdate()
	{
		if (moveTime >= 1f) return;
		moveTime = Mathf.Clamp01(moveTime + MyTime.deltaTime / moveOverSeconds);
		transform.position = Vector3.Lerp(fromPos, toPos, Easing.InOutQuad(moveTime));
		if (moveTime >= 1f)
		{
			moveFx.gameObject.SetActive(false);
			if (burying) gameObject.SetActive(false);
		}
	}

	public void Despawn()
	{
		laser.gameObject.SetActive(false);
		Bury();
	}

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
		minimapIcon.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		Reset();
	}

	private void Reset()
	{
		if (circleParent == null || zoneRenderer == null) return;
		circleParent.alpha = 0f;
		zonePropertyBlock = new MaterialPropertyBlock();
		zoneRenderer.GetPropertyBlock(zonePropertyBlock);
		zoneColor = new Color(0f, 1f, 1f, 1f);
		startColor = new Color(0f, 1f, 1f, 0f);
		zonePropertyBlock.SetColor("_MainColor", Color.clear);
		zoneRenderer.SetPropertyBlock(zonePropertyBlock);
		zoneRenderer.enabled = false;
		chargeProgress = 0f;
		completed = false;
		charging = false;
	}

	private void FindChargeTime()
	{
		ItemWrench wrench = MyPlayer.Instance.inventory.itemInventory.GetItem(EItem.Wrench) as ItemWrench;
		float minimum = chargeTime * 0.9f;
		currentChargeTime = wrench == null ? chargeTime : Mathf.Clamp(chargeTime + (chargeTime - minimum) / -10f * Mathf.Clamp(wrench.amount, 0, 10), minimum, chargeTime);
	}

	private void ChargeUpdate()
	{
		if (chargeProgress >= 1f) return;
		if (chargeProgress <= 0f && !charging) { chargeProgress = 0f; zoneRenderer.enabled = false; return; }
		chargeProgress += MyTime.deltaTime / (charging ? currentChargeTime : currentChargeTime * -0.25f);
		if (chargeProgress >= 1f) { chargeProgress = 1f; Complete(); }
		else if (chargeProgress <= 0f) chargeProgress = 0f;
		audioLoop.pitch = Mathf.Lerp(pitchStart, pitchEnd, chargeProgress);
		circleProgress.fillAmount = chargeProgress;
		zonePropertyBlock.SetColor("_MainColor", Color.Lerp(startColor, zoneColor, chargeProgress));
		zoneRenderer.SetPropertyBlock(zonePropertyBlock);
		if (chargeProgress <= 0f) zoneRenderer.enabled = false;
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
		GameObject particles = Instantiate(finishParticles, transform.position, Quaternion.identity);
		particles.GetComponent<ParticleSystem>().startColor = zoneColor;
		Invoke(nameof(ScorePopup), 0.2f);
		minimapIcon.SetActive(false);
		altarIcon.SetActive(false);
		laser.gameObject.SetActive(false);
		Bury();
		A_Charged?.Invoke(this);
	}

	private void ScorePopup()
	{
		UiManager.Instance.scoreUi.AddScore(LocalizationUtility.GetLocalizedString("Game_ScoreUi", "POPUP_PYLON"), "+1");
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
		FindChargeTime();
	}

	private void OnTriggerExit()
	{
		if (completed) return;
		circleParent.alpha = 0f;
		charging = false;
		audioAbort.Play();
		audioLoop.Stop();
	}
}
