using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Audio.Music;
using MilkShake;
using UnityEngine;
using Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;

public class GraveyardBossRoom : MonoBehaviour
{
	private enum ELavaState
	{
		Idle = 0,
		Rising = 1,
		UpIdle = 2,
		Lowering = 3
	}

	public Transform playerTeleportTransform;

	public Transform bossSpawnTransform;

	public InteractableGhostBossLeave interactableGhostBossLeave;

	private Enemy bossEnemy;

	private float spawnBossAtTime;

	public Light directionalLight;

	public AegisRenderer bossShields;

	public MusicTrack musicTrackBoss;

	private float timer;

	private static float darknessLightIntensityMultiplier;

	public static bool isPlayerInsideLight;

	public static Action A_BossDied;

	public BossLamp[] lamps;

	private int lampCount;

	private float lightDropFromLamps = 0.4f;

	private float currentLightIntensity;

	private float defaultLightIntensity;

	private Color defaultAmbientLight;

	private float bossDeadGracePeriod = 10f;

	private float bossDeadAtTime;

	public bool isBossDefeated;

	private float bdLightFadeDuration = 4f;

	private float bdLightStartFadeDelay = 0.25f;

	private float bdLightStartIntensity;

	private float bdLightIntensity;

	private float openGateDelay = 3f;

	private bool hasOpenedGate;

	public GraveyardBossMetalGate metalGate;

	private bool hasUsedDamageOtherThanLamps;

	public Transform lava;

	public Transform risingObjects;

	public Transform pillars;

	public AudioSource sfxRumble;

	public ShakePreset rumbleShake;

	private float lavaInterval = 30f;

	private float lavaDuration = 25f;

	private float lavaRiseTime = 4f;

	private float lavaStartTime;

	private float lavaEndTime;

	private float lavaRiseTimer;

	private Vector3 risingObjectsPositionDefault;

	private Vector3 risingObjectsPositionUp;

	private Vector3 lavaPosDefault;

	private Vector3 lavaPosUp;

	private Vector3 pillarsPosDefault;

	private Vector3 pillarsPosUp;

	private ELavaState lavaState;

	public bool isFightingBoss { get; private set; }

	public bool hasSpawnedBoss { get; private set; }

	private void Awake()
	{
		BossLamp.A_Activate += LampActivate;
		BossLamp.A_Deactivate += LampDeactivate;
		GhostKingAttackDarkness.A_LightIntensity += OnLightIntensityChangeFromDarknessAttack;
		GhostKingAttackDarkness.A_DarknessAttackSetEnemyTarget += DarknessAttackStarted;
		GhostKingAttackDarkness.A_Explode += DarknessExplosion;
		Enemy.A_EnemyReleasedFromPool += OnEnemyReleased;
		Enemy.A_Damage += OnEnemyDamage;
		Enemy.A_EnemyDied += OnEnemyDied;
	}

	private void OnDestroy()
	{
		BossLamp.A_Activate -= LampActivate;
		BossLamp.A_Deactivate -= LampDeactivate;
		GhostKingAttackDarkness.A_LightIntensity -= OnLightIntensityChangeFromDarknessAttack;
		GhostKingAttackDarkness.A_DarknessAttackSetEnemyTarget -= DarknessAttackStarted;
		GhostKingAttackDarkness.A_Explode -= DarknessExplosion;
		Enemy.A_EnemyReleasedFromPool -= OnEnemyReleased;
		Enemy.A_Damage -= OnEnemyDamage;
		Enemy.A_EnemyDied -= OnEnemyDied;
	}

	private void Start()
	{
	}

	private void LampActivate()
	{
		lampCount++;
		RefreshLighting();
		RefreshBossArmor();
	}

	private void LampDeactivate()
	{
		lampCount--;
		RefreshLighting();
		RefreshBossArmor();
	}

	private void RefreshBossArmor()
	{
		if (bossEnemy == null) return;
		float armor;
		switch (lampCount)
		{
			case 0: armor = 0.99f; break;
			case 1: armor = 0.95f; break;
			case 2: armor = 0.8f; break;
			case 3: armor = 0.6f; break;
			case 4: armor = 0f; break;
			default: armor = 0.98f; break;
		}
		bossEnemy.SetArmor(armor, lamps.Length - lampCount, lamps.Length);
		bossShields.SetAmount(lamps.Length - lampCount);
	}

	private void RefreshLighting()
	{
		currentLightIntensity = isBossDefeated
			? bdLightIntensity * darknessLightIntensityMultiplier
			: (1f - lightDropFromLamps + (float)lampCount / lamps.Length * lightDropFromLamps) * darknessLightIntensityMultiplier;
		directionalLight.intensity = currentLightIntensity * defaultLightIntensity;
		RenderSettings.ambientLight = Color.Lerp(Color.black, defaultAmbientLight, currentLightIntensity);
		RenderSettings.fogColor = Color.Lerp(Color.black, MapController.currentStage.fogColor, currentLightIntensity);
	}

	public void Activate()
	{
		gameObject.SetActive(true);
		isFightingBoss = true;
		isPlayerInsideLight = false;
		darknessLightIntensityMultiplier = 1f;
		defaultLightIntensity = MapController.currentStage.lightIntensity;
		defaultAmbientLight = MapController.currentStage.ambienceColor;
		MyTime.StartCryptBoss();
		lavaStartTime = MyTime.time + lavaInterval;
		lavaEndTime = lavaStartTime + lavaDuration;
		RefreshLighting();
		risingObjectsPositionDefault = risingObjects.position;
		risingObjectsPositionUp = risingObjectsPositionDefault + Vector3.up * 13f;
		lavaPosDefault = lava.position;
		lavaPosUp = lavaPosDefault + Vector3.up * 13f;
		pillarsPosDefault = pillars.position;
		pillarsPosUp = pillarsPosDefault + Vector3.up * 13f;
		spawnBossAtTime = MyTime.time + 3f;
	}

	private void BossFightOver()
	{
		if (isBossDefeated) return;
		isFightingBoss = false;
		bossEnemy = null;
		isBossDefeated = true;
		bossDeadAtTime = MyTime.time;
		foreach (var entry in EnemyManager.Instance.enemies) entry.Value.Kill();
		bdLightStartIntensity = currentLightIntensity;
		darknessLightIntensityMultiplier = 1f;
		interactableGhostBossLeave.OpenDoor();
		InteractableBossSpawner.A_BossDefeated?.Invoke(false);
	}

	private void PostBossDeadUpdate()
	{
		if (!isBossDefeated) return;
		float elapsed = MyTime.time - bossDeadAtTime;
		if (elapsed >= bdLightStartFadeDelay && bdLightIntensity < 1f)
		{
			float progress = Mathf.Clamp01((elapsed - bdLightStartFadeDelay) / bdLightFadeDuration);
			bdLightIntensity = Mathf.Lerp(bdLightStartIntensity, 1f, UnityEngine.UIElements.Experimental.Easing.InOutQuad(progress));
			RefreshLighting();
		}
		if (elapsed >= openGateDelay && !hasOpenedGate)
		{
			hasOpenedGate = true;
			metalGate.Open();
		}
	}

	private void Update()
	{
		timer += MyTime.deltaTime;
		CheckSpawnBoss();
		UpdateLava();
		UpdateShields();
		PostBossDeadUpdate();
	}

	private void FixedUpdate()
	{
		CheckIfBossDead();
		isPlayerInsideLight = false;
		foreach (BossLamp lamp in lamps)
			if (lamp.HasPlayer()) isPlayerInsideLight = true;
	}

	private void CheckIfBossDead()
	{
		if (!isBossDefeated && hasSpawnedBoss && bossEnemy != null && bossEnemy.IsDead()) BossFightOver();
	}

	private void UpdateShields()
	{
		Transform shieldParent = bossShields.transform.parent;
		if (bossEnemy != null)
		{
			if (!shieldParent.gameObject.activeSelf) shieldParent.gameObject.SetActive(true);
			shieldParent.position = bossEnemy.GetFeetPosition();
			shieldParent.Rotate(Vector3.up, 2f);
		}
		else if (shieldParent.gameObject.activeSelf) shieldParent.gameObject.SetActive(false);
	}

	private void CheckSpawnBoss()
	{
		if (hasSpawnedBoss || !isFightingBoss || MyTime.time < spawnBossAtTime) return;
		hasSpawnedBoss = true;
		bossEnemy = EnemyManager.Instance.SpawnBoss(EEnemy.GhostKing, 0, EEnemyFlag.StageBoss, bossSpawnTransform.position);
		RefreshBossArmor();
		foreach (BossLamp lamp in lamps) Physics.IgnoreCollision(bossEnemy.collider, lamp.GetComponent<Collider>(), true);
		MusicController.Instance.PlayMusicTrack(musicTrackBoss);
		UiManager.Instance.objective.OnBossSpawned();
	}

	private void OnEnemyReleased(Enemy enemy)
	{
		if (enemy == bossEnemy) BossFightOver();
	}

	private void OnEnemyDied(Enemy enemy, DamageContainer dc)
	{
		if (enemy == bossEnemy && enemy.enemyData.enemyName == EEnemy.GhostKing) A_BossDied?.Invoke();
	}

	private void OnLightIntensityChangeFromDarknessAttack(float i)
	{
		if (isBossDefeated) return;
		darknessLightIntensityMultiplier = i;
		RefreshLighting();
	}

	private void DarknessAttackStarted()
	{
		bossEnemy.FollowTarget(bossSpawnTransform);
	}

	private void DarknessExplosion()
	{
		foreach (BossLamp lamp in lamps)
			if (!lamp.HasPlayer()) lamp.Deactivate();
	}

	public static bool IsDarknessAttack()
	{
		return darknessLightIntensityMultiplier < 1f;
	}

	public static float GetDarknessIntensityMultiplier()
	{
		return darknessLightIntensityMultiplier;
	}

	private void OnEnemyDamage(Enemy enemy, DamageContainer dc)
	{
		if (dc.damageSource != "Unkown") hasUsedDamageOtherThanLamps = true;
	}

	private void OnDisable()
	{
		if (!isBossDefeated || directionalLight == null) return;
		directionalLight.intensity = defaultLightIntensity;
		RenderSettings.ambientLight = defaultAmbientLight;
		RenderSettings.fogColor = MapController.currentStage.fogColor;
	}

	private void FindNextLavaRise()
	{
		lavaStartTime = MyTime.time + lavaInterval;
		lavaEndTime = lavaStartTime + lavaDuration;
	}

	private void UpdateLava()
	{
		if (isBossDefeated)
		{
			if (lavaState == ELavaState.Idle) return;
			if (lavaState == ELavaState.UpIdle) BeginLavaMovement(ELavaState.Lowering);
		}
		if (lavaState == ELavaState.Idle && MyTime.time >= lavaStartTime)
			BeginLavaMovement(ELavaState.Rising);
		if (lavaState == ELavaState.UpIdle && MyTime.time >= lavaEndTime)
			BeginLavaMovement(ELavaState.Lowering);

		if (lavaState == ELavaState.Rising)
		{
			lavaRiseTimer += MyTime.deltaTime;
			float objectsProgress = LavaEase(lavaRiseTimer);
			float lavaProgress = LavaEase(lavaRiseTimer - 3f);
			float pillarsProgress = LavaEase(lavaRiseTimer - 4.5f);
			risingObjects.position = Vector3.Lerp(risingObjectsPositionDefault, risingObjectsPositionUp, objectsProgress);
			lava.position = Vector3.Lerp(lavaPosDefault, lavaPosUp, lavaProgress);
			pillars.position = Vector3.Lerp(pillarsPosDefault, pillarsPosUp, pillarsProgress);
			if (pillarsProgress >= 1f) lavaState = ELavaState.UpIdle;
		}
		else if (lavaState == ELavaState.Lowering)
		{
			lavaRiseTimer += MyTime.deltaTime;
			float lavaProgress = LavaEase(lavaRiseTimer);
			float pillarsProgress = LavaEase(lavaRiseTimer - 2f);
			float objectsProgress = LavaEase(lavaRiseTimer - 3.5f);
			risingObjects.position = Vector3.Lerp(risingObjectsPositionUp, risingObjectsPositionDefault, objectsProgress);
			lava.position = Vector3.Lerp(lavaPosUp, lavaPosDefault, lavaProgress);
			pillars.position = Vector3.Lerp(pillarsPosUp, pillarsPosDefault, pillarsProgress);
			if (objectsProgress >= 1f)
			{
				lavaState = ELavaState.Idle;
				FindNextLavaRise();
			}
		}
	}

	private float LavaEase(float elapsed)
	{
		return UnityEngine.UIElements.Experimental.Easing.InOutQuad(Mathf.Clamp01(elapsed / lavaRiseTime));
	}

	private void BeginLavaMovement(ELavaState state)
	{
		lavaState = state;
		lavaRiseTimer = 0f;
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.shaker != null && rumbleShake != null)
			PlayerCamera.Instance.shaker.Shake(rumbleShake);
		if (sfxRumble != null) sfxRumble.Play();
	}
}
