using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Actors.Enemies;
using Actors.Enemies;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDoDeathAnimation_003Ed__41 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		private float _003CanimationTime_003E5__2;

		private float _003Ctime_003E5__3;

		private Transform _003CplayerTransform_003E5__4;

		private Vector3 _003CrotationPoint_003E5__5;

		private Vector3 _003CrotationAxis_003E5__6;

		private Quaternion _003CinitialRotation_003E5__7;

		private Vector3 _003CradiusVector_003E5__8;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CDoDeathAnimation_003Ed__41(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public LayerMask whatIsGround;

	public LayerMask whatIsProjectileRaycast;

	public LayerMask whatIsEnemy;

	public LayerMask whatIsObjects;

	public LayerMask whatIsPlayer;

	public LayerMask whatIsGroundAndObjects;

	public LayerMask whatIsCameraCollision;

	public LayerMask whatIsBlockingRails;

	public LayerMask whatIsProjectileObstruction;

	private float gameTimer;

	public static GameManager Instance;

	public static Action A_StageStarted;

	public static Action A_RunStarted;

	public bool HasRaisedRunStarted { get; private set; }

	public static Action A_GameOver;

	public MyPlayer player;

	public PlayerCamera playerCamera;

	public UiManager uiManager;

	public int bossCurses;

	private bool inited;

	private bool hasSetMapDifficulty;

	private float nextGetStageTime;

	private float getMaxStageTimeInterval = 0.1f;

	private float lastFoundStageTime;

	public static Action A_DungeonStarted;

	public static Action A_DungeonEnded;

	private bool isPlaying;

	public bool cutscene;

	public bool isGameOver { get; private set; }

	public bool isCrypt { get; private set; }

	public bool isDungeonTimerStarted { get; private set; }

	public float dungeonTimeToComplete { get; private set; }

	public bool isDungeonOvertime { get; private set; }

	public int cryptIndex { get; private set; } = -1;

	private void Awake()
	{
		TryInit();
	}

	private void TryInit()
	{
		if (inited) return;
		inited = true;
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		PlayerHealth.A_Died += OnDied;
		EnemyManager.A_StageBossDied += OnStageBossDied;
		MyPlayer.A_PlayerInventoryInitialized += OnPlayerInit;
		if (MyPlayer.Instance != null && MyPlayer.Instance.hasStarted) OnPlayerInit(MyPlayer.Instance.inventory);
	}

	private void OnPlayerInit(PlayerInventory inventory)
	{
		if (hasSetMapDifficulty)
		{
			return;
		}
		hasSetMapDifficulty = true;
		if (MapController.currentMap == null || MapController.currentMap.eMap != EMap.Graveyard || inventory == null || inventory.statInventory == null)
		{
			return;
		}
		StatModifier stat = new StatModifier
		{
			stat = EStat.EnemyScalingMultiplier,
			modifyType = EStatModifyType.Multiplication,
			modification = 1.5f
		};
		inventory.statInventory.ChangeStat(stat, permanent: true, timeout: 0f, addToShrineLog: true);
	}

	public void CreateInstances()
	{
		TryInit();
		player.TryInit();
		playerCamera.TryInit();
		uiManager.TryInit();
	}

	public float GetStageTimeMax()
	{
		if (nextGetStageTime > MyTime.time) return lastFoundStageTime;
		nextGetStageTime = MyTime.time + getMaxStageTimeInterval;
		var summoner = EnemyManager.Instance != null ? EnemyManager.Instance.summonerController : null;
		if (summoner == null)
			return MapController.currentStage != null && MapController.currentStage.stageTimeline != null
				? MapController.currentStage.stageTimeline.GetStageTime() : 0f;
		float stageTime = summoner.GetStageTimeMax();
		if (ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_SPEEDRUN) && ChallengesTracker.challengeModifiers != null)
			foreach (var modifier in ChallengesTracker.challengeModifiers)
				if (modifier is ChallengeModifierSpeedrun speedrun && modifier.internalName == ChallengesTracker.MODIFIER_SPEEDRUN)
					stageTime = Mathf.Min(stageTime, speedrun.timeLeftOnStage);
		lastFoundStageTime = stageTime;
		return stageTime;
	}

	private void Start()
	{
		FinalFightController.isFightingFinalBoss = false;
		A_StageStarted?.Invoke();
		if (MapController.IsFirstStage())
		{
			HasRaisedRunStarted = true;
			A_RunStarted?.Invoke();
		}
	}

	private void OnDestroy()
	{
		ResumeEnemyGroundCollision();
		PlayerHealth.A_Died -= OnDied;
		EnemyManager.A_StageBossDied -= OnStageBossDied;
		MyPlayer.A_PlayerInventoryInitialized -= OnPlayerInit;
	}

	private void Update()
	{
		if (!isPlaying || GetPlayer() == null) return;
		gameTimer += MyTime.deltaTime;
		if (isCrypt && !isDungeonOvertime && MyTime.cryptTimer > dungeonTimeToComplete) StartDungeonOvertime();
	}

	public PlayerMovement GetPlayerMovement()
	{
		return PlayerMovement.Instance != null ? PlayerMovement.Instance : null;
	}

	public MyPlayer GetPlayer()
	{
		return MyPlayer.Instance != null ? MyPlayer.Instance : null;
	}

	public float GetAliveTime()
	{
		return gameTimer;
	}

	public PlayerInventory GetPlayerInventory()
	{
		return GetPlayer() == null ? null : GetPlayer().inventory;
	}

	private void OnDied()
	{
		isGameOver = true;
		A_GameOver?.Invoke();
		StartCoroutine(DoDeathAnimation());
	}

	public void OnTeleportAway()
	{
		isGameOver = true;
		A_GameOver?.Invoke();
	}

	private IEnumerator DoDeathAnimation()
	{
		cutscene = true;
		yield return new WaitForSeconds(0.1f);
		if (SaveManager.Instance != null) { SaveManager.Instance.SaveProgression(); SaveManager.Instance.SaveStats(); }
		MyPlayer currentPlayer = MyPlayer.Instance;
		currentPlayer.playerRenderer.ResetMaterial();
		MyTime.Pause();
		if (PlayerCamera.Instance != null && PlayerCamera.Instance.deathCamera != null) PlayerCamera.Instance.DeathCamera();
		if (UiManager.Instance != null && UiManager.Instance.deathScreen != null) UiManager.Instance.deathScreen.PlayAudio();
		Transform playerTransform = currentPlayer.transform;
		Vector3 rotationPoint = currentPlayer.playerMovement.GetRbFeetPosition() + Vector3.up * 0.2f;
		Vector3 rotationAxis = currentPlayer.playerRenderer.transform.forward;
		Quaternion initialRotation = playerTransform.rotation;
		Vector3 radius = playerTransform.position - rotationPoint;
		float time = 0f;
		while (time < 1f)
		{
			time = Mathf.Min(1f, time + Time.deltaTime / 2f);
			float angle = Mathf.Pow(time, 5f) * 90f;
			Quaternion rotation = Quaternion.AngleAxis(angle, rotationAxis);
			playerTransform.position = rotationPoint + rotation * radius;
			playerTransform.rotation = rotation * initialRotation;
			yield return null;
		}
		if (UiManager.Instance != null && UiManager.Instance.deathScreen != null) UiManager.Instance.deathScreen.StartDeathScreen();
	}

	public bool IsFinalSwarm()
	{
		return Assets.Scripts.Managers.EnemyManager.Instance != null && Assets.Scripts.Managers.EnemyManager.Instance.IsFinalSwarm();
	}

	public void StartDungeon(float timeToComplete)
	{
		cryptIndex++;
		isCrypt = true;
		isDungeonOvertime = false;
		dungeonTimeToComplete = timeToComplete;
		MyTime.cryptTimer = 0f;
		if (ChallengesTracker.HasChallengeModifier("crypt"))
		{
			ChallengeData challenge = ChallengesTracker.GetCurrentChallenge();
			if (challenge != null) dungeonTimeToComplete = challenge.targetValue;
		}
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Ground"), true);
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Object"), true);
		A_DungeonStarted?.Invoke();
	}

	public void StopDungeon()
	{
		isCrypt = false;
		isDungeonOvertime = false;
		foreach (var entry in EnemyManager.Instance.enemies)
			if (entry.Value != null && !entry.Value.IsDead()) entry.Value.ReleaseToPoolNextFrame();
		ResumeEnemyGroundCollision();
		A_DungeonEnded?.Invoke();
	}

	public void StartDungeonTimer()
	{
		isDungeonTimerStarted = true;
	}

	private void ResumeEnemyGroundCollision()
	{
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Ground"), false);
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Object"), false);
	}

	private void StartDungeonOvertime()
	{
		if (isDungeonOvertime) return;
		isDungeonOvertime = true;
		uiManager.alertUi.SetAlertTimesUp();
		Vector3 position = SpawnPositions.GetEnemySpawnPositionAroundPoint(MyPlayer.Instance.transform.position, 5f, 10f, 100, false, 999f);
		EnemyManager.Instance.SpawnBoss(EEnemy.GhostInvincible, 0, EEnemyFlag.Boss, position);
	}

	public static float GetViewDistance()
	{
		return 50f;
	}

	public static float GetViewDistanceSqr()
	{
		return 2500f;
	}

	public static float GetEnemyTeleportDistance()
	{
		return 75f;
	}

	public static float GetEnemyTeleportDistanceSqr()
	{
		return 5625f;
	}

	public bool IsTimeFreeze()
	{
		return MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze);
	}

	public bool IsPlaying()
	{
		return isPlaying;
	}

	public bool IsFinalBossDead()
	{
		return MapController.IsFinalBossStage() && EnemyManager.Instance != null && EnemyManager.Instance.stageBossIsDead;
	}

	public void StartPlaying()
	{
		isPlaying = true;
	}

	public bool HasEnteredBossRoom()
	{
		return RsgController.Instance != null && RsgController.Instance.roomBoss != null && RsgController.Instance.roomBoss.hasSpawnedBoss;
	}

	private void OnStageBossDied()
	{
		if (!MapController.IsFinalBossStage() || EnemyManager.Instance == null) return;
		foreach (Enemy enemy in new List<Enemy>(EnemyManager.Instance.enemies.Values))
			if (enemy != null) enemy.EnemyDied(null);
	}
}
