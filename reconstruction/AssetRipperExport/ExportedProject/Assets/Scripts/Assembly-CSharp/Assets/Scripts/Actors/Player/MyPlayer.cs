using System;
using System.Collections.Generic;
using Assets.Scripts.Camera;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Actors.Player
{
	public class MyPlayer : MonoBehaviour
	{
		public PlayerRenderer playerRenderer;

		public PlayerMovement playerMovement;

		public ParticleSystem levelupParticles;

		public AudioSource levelupSfx;

		public ECharacter character;

		public PlayerInput playerInput;

		public AuraAttacks playerConstantAttacks;

		public PlayerSfxs playerSfxs;

		public PlayerEffects playerEffects;

		public GameObject fogOfWar;

		public GameObject teleportEffect;

		public UnityEngine.Camera minimapCamera;

		public MinimapCamera minimapCameraScript;

		private bool inited;

		public PlayerInventory inventory;

		public static Action<PlayerInventory> A_PlayerInventoryInitialized;

		public static MyPlayer Instance;

		public static Action A_PrePlayerSpawn;

		public float height;

		public Transform feet;

		public Transform head;

		public bool hasStarted;

		public Vector3 spawnDir;

		public float width;

		private bool started;

		private float nextChangeTime;

		private bool isInvincible;

		private float damageCooldownOverAtTime;

		private float lastCalledTime;

		private Dictionary<GameObject, float> enemyCooldowns = new Dictionary<GameObject, float>();

		private float enemyAttackCooldown = 0.3f;

		private int enemyLayer;

		public static Action A_Collided;

		public static Action A_CollidedEnemy;

		private float movingDirectionBias;

		private float shootingDirectionBias;

		private float biasMoveSpeed = 0.015f;

		private Vector3 averageMovingDirection;

		private Vector3 averageMovingDirectionShooting;

		private float maxVectorSize = 4f;

		private float maxVectorSizeShooting = 3f;

		public Transform arrow;

		public Transform shootingArrow;

		public bool isTeleporting;

		private static float defaultBaseDamage = 8f;

		private float baseDamage = defaultBaseDamage;

		public float baseMovementSpeed { get; private set; }

		public void Awake()
		{
			TryInit();
		}

		public void TryInit()
		{
			if (inited) return;
			inited = true;
			if (Instance != null) { Destroy(gameObject); return; }
			Instance = this;
			enemyLayer = LayerMask.NameToLayer("Enemy");
			LevelupScreen.A_LevelUpClose += OnLevelUp;
			PlayerHealth.A_TakeDamage += OnDamage;
			PlayerHealth.A_CooldownOver += DamageCooldownOver;
			PlayerHealth.A_Died += OnPlayerDied;
		}

		private void Start()
		{
		}

		public void Spawn(Vector3 position, Vector3 direction, bool useHeightOffset = false)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			// Isolated, opt-in character diagnostic; the normal menu choice is unchanged.
			using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
				{
				if (intent.Call<bool>("getBooleanExtra", "megabonk_calcium_animation_probe", false)) CharacterMenu.selectedCharacter = ECharacter.Calcium;
				else if (intent.Call<bool>("getBooleanExtra", "megabonk_zapbar_probe", false)) CharacterMenu.selectedCharacter = ECharacter.TonyMcZoom;
				else
				{
					string requested = intent.Call<string>("getStringExtra", "megabonk_spawn_character");
					if (!string.IsNullOrEmpty(requested) && System.Enum.TryParse(requested, out ECharacter diagnosticCharacter) && System.Enum.IsDefined(typeof(ECharacter), diagnosticCharacter))
						CharacterMenu.selectedCharacter = diagnosticCharacter;
				}
			}
#endif
			A_PrePlayerSpawn?.Invoke();
			TryInit();
			gameObject.SetActive(true);
			character = CharacterMenu.selectedCharacter;
			StartPlayer(character, direction);
			playerInput.SetSpawnDirection(direction);
			height = GetComponent<CapsuleCollider>().height;
			Vector3 spawnPosition = position + Vector3.up * (height * 0.5f);
			transform.position = spawnPosition;
			GetComponent<Rigidbody>().position = spawnPosition;
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
			fogOfWar.transform.SetParent(null);
			fogOfWar.SetActive(true);
		}

		public void StartPlayer(ECharacter character, Vector3 direction)
		{
			CharacterData characterData = DataManager.Instance.GetCharacterData(character);
			inventory = MapController.GetPlayerInventory(characterData);
#if UNITY_ANDROID && !UNITY_EDITOR
			// The exported player hierarchy has the ZapBar controller inactive.
			// Enable it after inventory initialization so its original Start can reveal the Zap UI.
			foreach (var zapUi in GetComponentsInChildren<ZapBar>(true))
				zapUi.gameObject.SetActive(inventory.HasPassive(Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.EPassive.Zap));
#endif
			CalculateBaseDamage();
			playerRenderer.SetCharacter(characterData, inventory, direction);
			gameObject.SetActive(true);
			spawnDir = direction;
			RefreshSize(characterData, direction);
			if (UiManager.Instance != null) UiManager.Instance.colorFilterUi.ResetFilter();
			hasStarted = true;
			A_PlayerInventoryInitialized?.Invoke(inventory);
		}

		public void RefreshSize(CharacterData characterData, Vector3 direction, float sizeMultiplier = 1f)
		{
			CapsuleCollider capsule = GetComponent<CapsuleCollider>();
			inventory = MapController.GetPlayerInventory(characterData);
			width = characterData.colliderWidth * sizeMultiplier;
			capsule.height = characterData.colliderHeight * sizeMultiplier;
			capsule.radius = characterData.colliderWidth * sizeMultiplier;
			feet.position = transform.position + Vector3.down * (capsule.height * 0.5f);
			head.position = transform.position + Vector3.up * (capsule.height * 0.5f);
			playerRenderer.transform.position = feet.position;
			playerRenderer.transform.localScale = Vector3.one * sizeMultiplier;
			playerMovement.ResetState(character, direction);
			baseMovementSpeed = playerMovement.movementValues.maxRunSpeed;
		}

		private void FixedUpdate()
		{
			if (inventory == null) return;
			inventory.PhysicsTick();
			UpdateSpawnDirectionBias();
			if (isInvincible && MyTime.time >= damageCooldownOverAtTime) DamageCooldownOver();
		}

		private void Update()
		{
			inventory?.Update();
		}

		private void LateUpdate()
		{
			inventory?.LateUpdate();
		}

		private void OnDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
		{
			if (dc.knockback <= 0f) return;
			playerMovement.PushPlayer(dc.direction * dc.knockback);
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
			damageCooldownOverAtTime = MyTime.time + 0.15f;
			isInvincible = true;
		}

		private void DamageCooldownOver()
		{
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
			isInvincible = false;
		}

		public void OnLevelUp()
		{
			if (lastCalledTime >= MyTime.time) return;
			lastCalledTime = MyTime.time;
			CalculateBaseDamage();
			levelupParticles.gameObject.SetActive(true);
			levelupParticles.Play();
			levelupSfx.Stop();
			levelupSfx.Play();
			CancelInvoke(nameof(StopLevelupParticles));
			Invoke(nameof(StopLevelupParticles), 2f);
		}

		private void StopLevelupParticles()
		{
			levelupParticles.Stop();
			levelupParticles.gameObject.SetActive(false);
		}

		private void OnCollisionStay(Collision other)
		{
			A_Collided?.Invoke();
			if (!CanTakeDamage() || MyTime.paused || other.gameObject.layer != enemyLayer) return;
			if (enemyCooldowns.TryGetValue(other.gameObject, out float nextAttack) && nextAttack > MyTime.time) return;
			if (!EnemyManager.Instance.GetEnemy(other.collider, out Enemy enemy)) return;
			enemyCooldowns[other.gameObject] = MyTime.time + enemyAttackCooldown;
			inventory.playerHealth.DamagePlayer(enemy, transform.position - other.GetContact(0).point);
			var expired = new List<GameObject>();
			foreach (var entry in enemyCooldowns) if (entry.Value <= MyTime.time) expired.Add(entry.Key);
			foreach (var key in expired) enemyCooldowns.Remove(key);
			A_CollidedEnemy?.Invoke();
		}

		private void OnGUI()
		{
		}

		public bool CanTakeDamage()
		{
			return !isInvincible;
		}

		public void UpdateSpawnDirectionBias()
		{
			Vector3 velocity = playerMovement.GetVelocity();
			if (velocity.magnitude < 4f)
			{
				averageMovingDirection = Vector3.Lerp(averageMovingDirection, Vector3.zero, 0.02f);
				averageMovingDirectionShooting = Vector3.Lerp(averageMovingDirectionShooting, Vector3.zero, 0.06f);
				movingDirectionBias = Mathf.Clamp01(movingDirectionBias - biasMoveSpeed);
				return;
			}
			Vector3 direction = velocity.normalized;
			direction.y = 0f;
			averageMovingDirection += direction * 0.04f;
			averageMovingDirectionShooting += direction * 0.14f;
			if (averageMovingDirection.magnitude > maxVectorSize) averageMovingDirection = averageMovingDirection.normalized * maxVectorSize;
			if (averageMovingDirectionShooting.magnitude > maxVectorSizeShooting) averageMovingDirectionShooting = averageMovingDirectionShooting.normalized * maxVectorSizeShooting;
			movingDirectionBias = Mathf.Clamp01(movingDirectionBias + biasMoveSpeed);
		}

		public float GetSpawnDirectionBias()
		{
			float magnitude = averageMovingDirection.magnitude;
			return magnitude <= 1f ? 0f : Mathf.Clamp((magnitude - 1f) / (maxVectorSize - 1f), 0f, 0.4f);
		}

		public Vector3 GetAverageMovingDirection()
		{
			return averageMovingDirection;
		}

		public Vector3 GetAverageMovingDirectionTarget()
		{
			return averageMovingDirectionShooting;
		}

		public bool IsDead()
		{
			return inventory.playerHealth.IsDead();
		}

		private void OnPlayerDied()
		{
			if (SaveManager.Instance != null && SaveManager.Instance.progression != null && MapController.currentMap != null)
				SaveManager.Instance.progression.menuMeta.GetMapProgress(MapController.currentMap.eMap)
					.SetKills(Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats.GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.kills));
		}

		public void TeleportPlayerNextStage()
		{
			teleportEffect.SetActive(true);
			isTeleporting = true;
			Invoke(nameof(TeleportEnd), 1f);
		}

		public void TeleportPlayerImmediate(Vector3 position, Vector3 dir, Vector3 cameraDir, float cameraPitch = 20f)
		{
			playerMovement.TeleportPlayer(position);
			playerRenderer.ForceRotation(dir);
			playerInput.SetSpawnDirection(cameraDir, cameraPitch);
		}

		private void TeleportEnd()
		{
			inventory.statusEffects.RemoveAllStatusEffects();
			teleportEffect.transform.SetParent(null);
			gameObject.SetActive(false);
		}

		public void PauseInventory(bool b)
		{
			inventory.pause = b;
		}

		public Vector3 GetFeetPosition()
		{
			return transform.position + Vector3.down * (height * 0.5f);
		}

		private void CalculateBaseDamage()
		{
			baseDamage = defaultBaseDamage + inventory.GetCharacterLevel();
		}

		public float GetBaseDamage()
		{
			return baseDamage;
		}

		private void OnDestroy()
		{
			if (Instance != this) return;
			LevelupScreen.A_LevelUpClose -= OnLevelUp;
			PlayerHealth.A_TakeDamage -= OnDamage;
			PlayerHealth.A_CooldownOver -= DamageCooldownOver;
			PlayerHealth.A_Died -= OnPlayerDied;
		}
	}
}
