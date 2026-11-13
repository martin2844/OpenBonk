using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableCoffin : BaseInteractable
{
	public const int numCoffins = 4;

	public GameObject rayFx;

	public Animator coffinAnimator;

	public LocalizedString interactString;

	public Transform bossSpawnPos;

	public GameObject zone;

	public GameObject minimapIcon;

	public GameObject keyPickup;

	public AudioSource ambience;

	private static int currentGhostIndex;

	private HashSet<Enemy> minibossEnemies = new HashSet<Enemy>();

	private static bool hasActiveFight;

	private bool interacted;

	[Header("Animating and FX")]
	public AudioSource audio;

	public ParticleSystem[] stormParticles;

	public MeshRenderer fogOfWarRenderer;

	public GameObject zoneCollider;

	public GameObject zoneRenderer;

	private Material fogOfWarMaterial;

	private float fogIntensityDefault;

	private float fogIntensityNew = 0.015f;

	private Color fogColorDefault;

	private Color fogColorNew;

	private float audioVolume;

	private float fadeOverTime = 3f;

	private float fadeZoneTime = 3f;

	private float fadeTime;

	private bool isActive;

	private bool startedAnimating;

	private void Awake()
	{
		currentGhostIndex = 0;
		hasActiveFight = false;
		if (audio != null) audioVolume = audio.volume;
		if (fogOfWarRenderer != null) fogOfWarMaterial = fogOfWarRenderer.material;
#if UNITY_ANDROID && !UNITY_EDITOR
		// Each coffin owns its material instance, so GPU instancing cannot batch these spheres.
		if (fogOfWarMaterial != null) fogOfWarMaterial.enableInstancing = false;
#endif
		Enemy.A_EnemyReleasedFromPool += OnEnemyDied;
		DetectInteractables.A_Interacted += OnInteracted;
	}

	private new void Start()
	{
		base.Start();
		if (MapController.currentStage != null)
		{
			fogIntensityDefault = MapController.currentStage.fogIntensity;
			fogColorDefault = MapController.currentStage.fogColor;
		}
	}

	private new void OnDestroy()
	{
		Enemy.A_EnemyReleasedFromPool -= OnEnemyDied;
		DetectInteractables.A_Interacted -= OnInteracted;
		base.OnDestroy();
	}

	public override bool Interact()
	{
		if (interacted) return false;
		interacted = true;
		hasActiveFight = true;
		if (coffinAnimator != null) coffinAnimator.Play("CoffinOpen");
		if (zoneCollider != null) zoneCollider.SetActive(true);
		if (rayFx != null) rayFx.SetActive(false);
		Invoke(nameof(FadeIn), 0.3f);
		if (minimapIcon != null) minimapIcon.SetActive(false);
		EnemyData data = GetEnemyData();
		int count = GameManager.Instance != null ? GameManager.Instance.bossCurses + 1 : 1;
		for (int i = 0; i < count; i++)
		{
			Enemy enemy = data != null && EnemyManager.Instance != null && bossSpawnPos != null
				? EnemyManager.Instance.SpawnBoss(data.enemyName, 0, EEnemyFlag.Boss, bossSpawnPos.position) : null;
			if (enemy != null) minibossEnemies.Add(enemy);
			else Debug.LogError("Could not spawn Graveyard coffin ghost.");
		}
		currentGhostIndex++;
		return true;
	}

	private void OnEnemyDied(Enemy enemy)
	{
		if (!minibossEnemies.Remove(enemy) || minibossEnemies.Count != 0) return;
		if (keyPickup != null)
		{
			keyPickup.SetActive(true);
			if (UiManager.Instance != null && UiManager.Instance.objectiveArrow != null)
				UiManager.Instance.objectiveArrow.SetTarget(keyPickup.transform, Vector3.up * 4.5f, 0f, 999999f, 1f);
		}
	}

	private void OnKeyPickedUp()
	{
	}

	private void OnZoneCharged()
	{
	}

	private void OnInteracted(BaseInteractable interactable, bool succeess)
	{
		if (!succeess || interactable == null || keyPickup == null || interactable.gameObject != keyPickup) return;
		if (zone != null) zone.SetActive(false);
		if (fogOfWarRenderer != null) fogOfWarRenderer.gameObject.SetActive(false);
		hasActiveFight = false;
		FadeOut();
		if (UiManager.Instance != null && UiManager.Instance.objectiveArrow != null)
			UiManager.Instance.objectiveArrow.ClearTarget();
	}

	public override string GetInteractString()
	{
		return interactString != null ? interactString.GetLocalizedString() : string.Empty;
	}

	public override bool CanInteract()
	{
		return !interacted && !hasActiveFight;
	}

	public EnemyData GetEnemyData()
	{
		EEnemy enemy = EEnemy.GhostGrave1;
		switch (currentGhostIndex)
		{
			case 1: enemy = EEnemy.GhostGrave2; break;
			case 2: enemy = EEnemy.GhostGrave3; break;
			case 3: enemy = EEnemy.GhostGrave4; break;
		}
		return DataManager.Instance != null ? DataManager.Instance.GetEnemyData(enemy) : null;
	}

	public void FadeIn()
	{
		isActive = true;
		startedAnimating = true;
		if (fogOfWarRenderer != null) fogOfWarRenderer.enabled = true;
		if (MapController.currentStage != null)
		{
			fogIntensityDefault = MapController.currentStage.fogIntensity;
			fogColorDefault = MapController.currentStage.fogColor;
		}
		fogColorNew = Color.Lerp(fogColorDefault, Color.black, 0.65f);
		if (MyPlayer.Instance != null && MyPlayer.Instance.playerMovement != null)
		{
			Vector3 direction = (MyPlayer.Instance.transform.position - transform.position).normalized;
			MyPlayer.Instance.playerMovement.PushPlayer((direction + Vector3.up * 0.5f).normalized * 22f);
		}
		fadeTime = 0f;
		if (audio != null) { audio.volume = 0f; audio.Play(); }
		SetStormEmission(true);
	}

	public void FadeOut()
	{
		fadeTime = 0f;
		isActive = false;
		SetStormEmission(false);
	}

	private void Update()
	{
		if (!startedAnimating) return;
		if (fadeTime < fadeOverTime)
		{
			fadeTime = Mathf.Clamp(fadeTime + MyTime.deltaTime, 0f, fadeOverTime);
			float t = fadeTime / fadeOverTime;
			float amount = isActive ? t : 1f - t;
			if (fogOfWarMaterial != null) fogOfWarMaterial.SetColor("_Color", new Color(0f, 0f, 0f, amount));
			if (audio != null) audio.volume = audioVolume * amount;
			RenderSettings.fogDensity = Mathf.Lerp(fogIntensityDefault, fogIntensityNew, amount);
			RenderSettings.fogColor = Color.Lerp(fogColorDefault, fogColorNew, amount);
		}
		else if (!isActive && audio != null) audio.Stop();
		if (zoneRenderer != null)
			zoneRenderer.transform.localScale = Vector3.one * UnityEngine.UIElements.Experimental.Easing.OutCirc(Mathf.Clamp01(fadeTime / fadeZoneTime));
		if (fadeTime >= fadeOverTime && !isActive && interacted) enabled = false;
	}

	private void SetStormEmission(bool value)
	{
		if (stormParticles == null) return;
		foreach (ParticleSystem particle in stormParticles)
			if (particle != null) { var emission = particle.emission; emission.enabled = value; }
	}

	public InteractableCoffin() { showOutline = true; }
}
