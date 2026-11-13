using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts.Objects.Pooling;
using Utility;
using UnityEngine;

namespace Assets.Scripts.Actors.Enemies
{
	public class Enemy : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CDespawn_003Ed__111 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public Enemy _003C_003E4__this;

			private float _003Ctime_003E5__2;

			private float _003CdesiredHeight_003E5__3;

			private Vector3 _003ClocalPos_003E5__4;

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
			public _003CDespawn_003Ed__111(int _003C_003E1__state)
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

		[CompilerGenerated]
		private sealed class _003CStartTeleporting_003Ed__110 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public Enemy _003C_003E4__this;

			public bool skipStart;

			public Vector3 toPosition;

			private float _003Ctime_003E5__2;

			private float _003CdesiredHeight_003E5__3;

			private Vector3 _003ClocalPos_003E5__4;

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
			public _003CStartTeleporting_003Ed__110(int _003C_003E1__state)
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

		public AnimatedMesh animatedMesh;

		public Renderer renderer;

		public EnemyRenderer enemyRenderer;

		public Rigidbody rb;

		public CapsuleCollider collider;

		public EnemyMovementRb enemyMovement;

		public Material whiteMaterial;

		public EnemyDissolve dissolve;

		private float flashTime = 0.08f;

		public float maxHp;

		private float despawnAtTime;

		public EEnemyState state;

		private float eliteScaleMultiplier = 1.5f;

		private SpecialAttackController specialAttackController;

		public static int deaths;

		public static Action<Enemy, DamageContainer> A_EnemyDied;

		public static Action<Enemy> A_EnemyDiedPre;

		public static Action<Enemy> A_EnemySpawned;

		public static Action<Enemy> A_EnemyReleasedFromPool;

		public static Action<Enemy> A_TargetOfInterestSpawn;

		public static Action<Enemy, DamageContainer> A_Damage;

		public Action<Enemy, DamageContainer> A_DamageNonStatic;

		public static Action<Enemy> A_HealthChange;

		private float controlHp;

		private EEnemyFlag enemyFlag;

		private float maxDespawnTime = 30f;

		private float speedMultiplier = 1f;

		private EnemyStatusSymbols statusSymbols;

		private float armor;

		public static Action<Enemy, int, int> A_ArmorChanged;

		private EEnemyFlag eliteChallengeFlags = EEnemyFlag.Elite | EEnemyFlag.Challenge;

		private Vector3 defaultScale = Vector3.one;

		private Outline outline;

		public float teleportTime = 0.75f;

		public static float bossTeleportTime = 2f;

		public static float defaultTeleportTime = 0.75f;

		public static Action A_HpTamper;

		private float echoDamage;

		private float stopFlashTime;

		private float readyToFlashTime;

		public float flashInterval = 0.14f;

		private bool flashing;

		private bool isInvulnerable;

		public static Action<Enemy, bool> A_InvulnerableChanged;

		private bool isDyingNextFrame;

		private bool deathFunctionCalled;

		private float startTeleportThresholdDistance;

		private float lastTeleportTime;

		private List<AddDebuffContainer> _toAddBuffer = new List<AddDebuffContainer>();

		public Dictionary<EDebuff, EnemyDebuff> debuffs = new Dictionary<EDebuff, EnemyDebuff>();

		public HashSet<EDebuff> debuffsToRemove = new HashSet<EDebuff>();

		public Dictionary<EDebuff, AddDebuffContainer> debuffsToAdd = new Dictionary<EDebuff, AddDebuffContainer>();

		public Action<EDebuff> A_DebuffAdded;

		public Action<EDebuff> A_DebuffRemoved;

		private Dictionary<EDebuff, int> debuffCounts = new Dictionary<EDebuff, int>
		{
			[EDebuff.Stun] = 0, [EDebuff.Freeze] = 0, [EDebuff.Charm] = 0
		};

		private float nextVerifyTime;

		private float nextTeleportTimeCheck;

		private float teleportCheckInterval;

		private float minStayAtDistance;

		private float maxStayAtDistance;

		private bool allowSpecialAttacks;

		private float basePowerupDropChance = 0.01f;

		public EnemyData enemyData { get; private set; }

		public float meshHeight { get; private set; }

		public float meshRadius { get; private set; }

		public Vector3 feetOffset { get; private set; }

		public float hp { get; set; }

		internal void SetDiagnosticHealth(float value)
		{
			hp = controlHp = value;
		}

		public uint id { get; private set; }

		public int waveNumber { get; set; }

		public Rigidbody target { get; private set; }

		public float spawnedAtTime { get; private set; }

		public float extraKnockbackRes { get; set; }

		public int armorCurrent { get; private set; }

		public int armorMax { get; private set; }

		public Transform followTarget { get; private set; }

		public void InitEnemy(uint id, EnemyData enemyData, Vector3 pos, int waveNumber, EEnemyFlag flag = EEnemyFlag.None, bool canBeElite = true, float extraSizeMultiplier = 1f)
		{
			// Core of the original pooled-enemy initialization. Teleport, status
			// effects, boss-specific setup and special attacks still need recovery.
			deathFunctionCalled = false;
			gameObject.SetActive(true);
			gameObject.layer = LayerMask.NameToLayer("Enemy");
			this.enemyData = enemyData;
			this.id = id;
			this.waveNumber = waveNumber;
			spawnedAtTime = MyTime.time;
			isInvulnerable = false;
			isDyingNextFrame = false;
			speedMultiplier = 1f;
			echoDamage = 0f;
			enemyFlag = flag;
			armor = 0f;
			armorCurrent = armorMax = 0;
			A_ArmorChanged?.Invoke(this, 0, 0);
			extraKnockbackRes = 0f;
			if (!IsElite() && canBeElite && MyRandom.random.NextDouble() < EnemyStats.GetEliteChance(enemyData))
				enemyFlag |= EEnemyFlag.Elite;
			float size = PlayerStats.GetStat(EStat.EnemySizeMultiplier) * enemyData.rendererScale;
			if (IsElite()) size *= eliteScaleMultiplier;
			animatedMesh.SetAnimation(enemyData.animation);
			Transform renderTransform = renderer.transform;
			renderTransform.localScale = Vector3.one;
			renderTransform.localRotation = Quaternion.Euler(enemyData.rendererRotationOffset);
			enemyRenderer.Set(enemyData);
			dissolve.enabled = false;
			float renderHeight = renderer.bounds.size.y;
			size *= extraSizeMultiplier;
			renderTransform.localPosition = Vector3.down * (enemyData.rendererOffset.y + renderHeight * 0.5f) * size +
				new Vector3(enemyData.rendererOffset.x, 0f, enemyData.rendererOffset.z) * size;
			renderTransform.localScale = Vector3.one * size;
			Bounds bounds = renderer.bounds;
			meshHeight = enemyData.overrideHeight > 0f ? enemyData.overrideHeight * size : bounds.size.y;
			meshRadius = Mathf.Max(bounds.size.x, bounds.size.z);
			dissolve.Reset();
			dissolve.A_DissolveFinished -= OnDissolveFinished;
			dissolve.A_DissolveFinished += OnDissolveFinished;
			rb.mass = enemyData.mass;
			transform.position = pos + Vector3.up * (meshHeight * 0.5f);
			rb.position = transform.position;
			enemyMovement.Init();
			if (MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect.TimeFreeze)) { animatedMesh.Pause(); enemyMovement.StopMovement(); }
			collider.radius = enemyData.colliderRadius * size;
			collider.height = meshHeight;
			collider.center = enemyData.colliderCenter != Vector3.zero ? enemyData.colliderCenter * size : Vector3.zero;
			hp = controlHp = maxHp = EnemyStats.GetHp(this);
			target = GameManager.Instance.GetPlayerMovement().GetRb();
			despawnAtTime = 0f;
			state = EEnemyState.Default;
			teleportTime = defaultTeleportTime;
			ClearAllDebuffs();
			allowSpecialAttacks = true;
			if (flag == EEnemyFlag.StageBoss || flag == EEnemyFlag.Boss)
			{
				CheckStatusSymbols();
				teleportTime = bossTeleportTime;
				enemyMovement.Init();
			}
			else if (flag == EEnemyFlag.SummonerMiniboss) SetSummonerMiniboss();
			specialAttackController = enemyData.specialAttacks.Length > 0 ? new SpecialAttackController(this) : null;
			startTeleportThresholdDistance = GameManager.GetEnemyTeleportDistance() * UnityEngine.Random.Range(1f, 2f);
			teleportCheckInterval = UnityEngine.Random.Range(1, 3) * 5f;
			nextTeleportTimeCheck = MyTime.time + teleportCheckInterval;
			StartCoroutine(StartTeleporting(transform.position, true));
			if (enemyData.minStayAtDistance > 0f)
			{
				float distanceScale = UnityEngine.Random.Range(0.75f, 1.5f);
				minStayAtDistance = enemyData.minStayAtDistance * distanceScale;
				maxStayAtDistance = enemyData.maxStayAtDistance * distanceScale;
				startTeleportThresholdDistance = maxStayAtDistance * 1.5f;
			}
			debuffCounts = new Dictionary<EDebuff, int> { [EDebuff.Stun] = 0, [EDebuff.Freeze] = 0 };
			CheckStatusSymbols();
			A_EnemySpawned?.Invoke(this);
			if (IsBoss()) A_TargetOfInterestSpawn?.Invoke(this);
		}

		public void SetSpeedMultiplier(float f)
		{
			speedMultiplier = f;
		}

		public void SetSwarmMultiplierHp(float f)
		{
			float scaled = hp * f;
			hp = maxHp = controlHp = float.IsNaN(scaled) || float.IsInfinity(scaled) ? float.MaxValue : Mathf.Max(0f, scaled);
		}

		public float GetSpeed()
		{
			if (enemyData.minStayAtDistance > 0f && enemyMovement.distanceToTarget > minStayAtDistance &&
				enemyMovement.distanceToTarget < maxStayAtDistance) return 0f;
			float challengeMultiplier = (enemyFlag & EEnemyFlag.Challenge) != 0 ? 1.1f : 1f;
			return EnemyStats.GetSpeed(enemyData) * challengeMultiplier * speedMultiplier;
		}

		public float GetExtraKnockback()
		{
			return extraKnockbackRes;
		}

		public int GetMoney()
		{
			return 1;
		}

		private void Freeze()
		{
			animatedMesh.Pause();
			enemyMovement.StopMovement();
		}

		private void UnFreeze()
		{
			animatedMesh.UnPause();
			enemyMovement.StartMovement();
		}

		private void CheckScale()
		{
			renderer.transform.localScale = defaultScale;
		}

		public void MakeChallenge()
		{
			CheckStatusSymbols();
		}

		private void CheckStatusSymbols()
		{
			bool isElite = (enemyFlag & EEnemyFlag.Elite) != 0;
			bool isBoss = (enemyFlag & EEnemyFlag.AnyBoss) != 0;
			bool isChallenge = (enemyFlag & EEnemyFlag.Challenge) != 0;
			if (!isElite && !isBoss && !isChallenge) { RemoveStatusSymbols(); return; }
			if (statusSymbols == null) AddStatusSymbols();
			if (statusSymbols != null) statusSymbols.Set(isElite, isBoss, isChallenge);
		}

		public void Heal(int amount)
		{
			hp = Mathf.Min(hp + amount, maxHp);
			controlHp = hp;
			if (EffectManager.Instance != null)
				EffectManager.Instance.PopupText("+" + amount, new Color(0f, 1f, 0f, 0f), transform.position + Vector3.up, 18);
			A_HealthChange?.Invoke(this);
		}

		private void SetBoss()
		{
			CheckStatusSymbols();
			teleportTime = bossTeleportTime;
			enemyMovement.Init();
		}

		public void SetArmor(float newArmor, int current, int max)
		{
			armor = newArmor;
			armorCurrent = current;
			armorMax = max;
			A_ArmorChanged?.Invoke(this, current, max);
		}

		public void SetSummonerMiniboss()
		{
			SetBoss();
			float multiplier;
			switch (Assets.Scripts.Managers.EnemyManager.Instance.summonerController.minibossCount)
			{
				case 0: multiplier = 3f; break;
				case 1: multiplier = 9f; break;
				case 2: multiplier = 20f; break;
				case 3: multiplier = 40f; break;
				default: multiplier = 1f; break;
			}
			controlHp = EnemyStats.GetHp(this) * multiplier;
			hp = maxHp = controlHp = EnemyStats.GetHp(this) * multiplier;
			enemyMovement.Init();
			A_HealthChange?.Invoke(this);
		}

		public bool IsStageBoss()
		{
			return (enemyFlag & EEnemyFlag.StageBoss) != 0;
		}

		public bool IsBoss()
		{
			return (enemyFlag & EEnemyFlag.AnyBoss) != 0;
		}

		public bool IsElite()
		{
			return (enemyFlag & EEnemyFlag.Elite) != 0;
		}

		public bool IsChallenge()
		{
			return (enemyFlag & EEnemyFlag.Challenge) != 0;
		}

		public bool IsEliteChallenge()
		{
			return (enemyFlag & eliteChallengeFlags) == eliteChallengeFlags;
		}

		public bool IsFinalBoss()
		{
			return (enemyFlag & EEnemyFlag.FinalBoss) != 0;
		}

		public void SetMinibossGoon(float hp)
		{
			this.hp = maxHp = controlHp = hp;
		}

		private void AddStatusSymbols()
		{
			if (statusSymbols != null || PoolManager.Instance == null || PoolManager.Instance.enemyStatusSymbolsPool == null || renderer == null) return;
			GameObject obj = PoolManager.Instance.enemyStatusSymbolsPool.Get();
			if (obj == null) return;
			statusSymbols = obj.GetComponent<EnemyStatusSymbols>();
			if (statusSymbols == null) { PoolManager.Instance.enemyStatusSymbolsPool.Release(obj); return; }
			obj.SetActive(true);
			Transform marker = statusSymbols.transform;
			marker.SetParent(null);
			marker.localScale = Vector3.one;
			marker.position = renderer.transform.position + Vector3.up * (meshHeight + 0.5f);
			marker.SetParent(renderer.transform);
		}

		private void RemoveStatusSymbols()
		{
			if (statusSymbols == null) return;
			GameObject obj = statusSymbols.gameObject;
			obj.transform.SetParent(null);
			obj.transform.localScale = Vector3.one;
			obj.SetActive(false);
			if (PoolManager.Instance != null && PoolManager.Instance.enemyStatusSymbolsPool != null)
				PoolManager.Instance.enemyStatusSymbolsPool.Release(obj);
			statusSymbols = null;
		}

		private void ResetUi()
		{
		}

		private void Awake()
		{
			MyTime.A_Pause += OnPaused;
			PlayerStatusEffects.A_StatusEffectAdded += OnStatusEffectAdded;
			PlayerStatusEffects.A_StatusEffectRemoved += OnStatusEffectRemoved;
		}

		private void OnDestroy()
		{
			MyTime.A_Pause -= OnPaused;
			PlayerStatusEffects.A_StatusEffectAdded -= OnStatusEffectAdded;
			PlayerStatusEffects.A_StatusEffectRemoved -= OnStatusEffectRemoved;
		}

		private void OnStatusEffectAdded(EStatusEffect eStatusEffect, bool newEffect)
		{
			if (eStatusEffect != EStatusEffect.TimeFreeze) return;
			animatedMesh.Pause();
			enemyMovement.StopMovement();
		}

		private void OnStatusEffectRemoved(EStatusEffect eStatusEffect)
		{
			if (eStatusEffect != EStatusEffect.TimeFreeze) return;
			animatedMesh.UnPause();
			enemyMovement.StartMovement();
		}

		private IEnumerator StartTeleporting(Vector3 toPosition, bool skipStart = false)
		{
			if (state == EEnemyState.Teleporting) yield break;
			collider.enabled = false;
			enemyMovement.StopMovement();
			state = EEnemyState.Teleporting;
			float height = meshHeight * -1.25f;
			Vector3 original = renderer.transform.localPosition;
			float progress = 0f;
			if (!skipStart)
			{
				while (progress < 1f)
				{
					progress += MyTime.deltaTime / teleportTime;
					renderer.transform.localPosition = original + Vector3.up * Mathf.Lerp(0f, height, UnityEngine.UIElements.Experimental.Easing.InOutQuad(progress));
					yield return null;
				}
			}
			transform.position = toPosition;
			renderer.transform.localPosition = original + Vector3.up * height;
			Vector3 facing = target.transform.position - transform.position;
			facing.y = 0f;
			transform.rotation = Quaternion.LookRotation(facing);
			enemyMovement.SetDesiredRotation(target.transform.position);
			GameObject fx = PoolManager.Instance.enemySpawnFxPool.Get();
			if (fx != null) fx.GetComponent<EnemySpawnParticles>().Set(this);
			progress = 0f;
			while (progress < 1f)
			{
				progress += MyTime.deltaTime / teleportTime;
				renderer.transform.localPosition = original + Vector3.up * Mathf.Lerp(height, 0f, UnityEngine.UIElements.Experimental.Easing.InOutQuad(progress));
				yield return null;
			}
			state = EEnemyState.Default;
			renderer.transform.localPosition = original;
			collider.enabled = true;
			if (!MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze)) enemyMovement.StartMovement();
		}

		private IEnumerator Despawn()
		{
			if (state == EEnemyState.Teleporting || hp <= 0f) yield break;
			collider.enabled = false;
			enemyMovement.StopMovement();
			state = EEnemyState.Teleporting;
			float progress = 0f;
			float height = meshHeight * -1.25f;
			Vector3 original = renderer.transform.localPosition;
			GameObject fx = PoolManager.Instance.enemySpawnFxPool.Get();
			if (fx != null) fx.GetComponent<EnemySpawnParticles>().Set(this);
			while (progress < 1f)
			{
				progress += MyTime.deltaTime / teleportTime;
				renderer.transform.localPosition = original + Vector3.up * Mathf.Lerp(0f, height, UnityEngine.UIElements.Experimental.Easing.InOutQuad(progress));
				yield return null;
			}
			state = EEnemyState.Default;
			renderer.transform.localPosition = original;
			collider.enabled = true;
			enemyMovement.StartMovement();
			ReleaseToPool();
		}

		public void DamageFromPlayerWeapon(DamageContainer dc)
		{
			Damage(dc);
			if (dc.procCoefficient > 0f) MyPlayer.Instance.inventory.itemInventory.PostDamage(dc);
		}

		public void DamageFromPlayerOther(DamageContainer dc)
		{
			Damage(dc);
			if (dc.procCoefficient > 0f) MyPlayer.Instance.inventory.itemInventory.PostDamage(dc);
		}

		public void DamageExternal(DamageContainer dc)
		{
			Damage(dc);
		}

		public bool HasDebuff(EDebuff debuff)
		{
			return debuffs.ContainsKey(debuff);
		}

		public void ReleaseEcho()
		{
			if (echoDamage <= 0f) return;
			var dc = new DamageContainer(0f, EDebuff.Echo.ToString()) { damage = echoDamage, damageEffect = Assets.Scripts.Game.Combat.EDamageEffect.Echo, enemy = this };
			DamageFromPlayerWeapon(dc);
			echoDamage = 0f;
		}

		private void Damage(DamageContainer dc)
		{
			if (!CanTakeDamage()) return;
			if (Mathf.Abs(controlHp - hp) > 2f) A_HpTamper?.Invoke();
			if (IsElite()) dc.damage *= PlayerStats.GetStat(EStat.EliteDamageMultiplier);
			if (HasDebuff(EDebuff.Echo)) { echoDamage += dc.damage; return; }
			if (armor > 0f && (dc.flags & DcFlags.BypassAll) != DcFlags.BypassAll) dc.damage *= 1f - armor;
			hp = controlHp = Mathf.Clamp(hp - dc.damage, 0f, float.MaxValue);
			if (hp <= 0f) EnemyDied(dc);
			else
			{
				if (SaveManager.Instance != null && SaveManager.Instance.config?.cfVisualsSettings != null &&
					SaveManager.Instance.config.cfVisualsSettings.damage_flash == 1 && MyTime.time > readyToFlashTime)
				{
					if (renderer != null) renderer.sharedMaterial = whiteMaterial;
					flashing = true;
					stopFlashTime = MyTime.time + flashTime;
					readyToFlashTime = stopFlashTime + flashInterval;
				}
			}
			if (enemyData.despawnTime > 0f && despawnAtTime <= 0f) despawnAtTime = MyTime.time + enemyData.despawnTime;
			if (EffectManager.Instance != null) EffectManager.Instance.NewDamageNumbers(dc, this);
			if (dc.knockback > 0f) enemyMovement.Knockback(dc);
			A_Damage?.Invoke(this, dc);
			A_DamageNonStatic?.Invoke(this, dc);
		}

		public void MakeWhite()
		{
			renderer.sharedMaterial = whiteMaterial;
			CancelInvoke(nameof(ResetMaterial));
		}

		public void Kill(string damageSource = "Unkown")
		{
			Damage(new DamageContainer(0f, damageSource) { damage = hp + 1f, direction = Vector3.zero, knockback = 0f });
		}

		public bool CanTakeDamage()
		{
			return hp > 0f && state != EEnemyState.Teleporting && !isInvulnerable;
		}

		public void MakeInvulnerable(bool invulnerable)
		{
			isInvulnerable = invulnerable;
			if (enemyRenderer != null) enemyRenderer.SetInvulnerable(invulnerable);
			QueueClearAllDebuffs();
			A_InvulnerableChanged?.Invoke(this, invulnerable);
		}

		public void DiedNextFrame()
		{
			isDyingNextFrame = true;
			Invoke(nameof(EnemyDied), 0f);
		}

		private void EnemyDied()
		{
			EnemyDied(new DamageContainer(0f, DamageContainer.unknownDamageSource));
		}

		public void EnemyDied(DamageContainer dc)
		{
			if (deathFunctionCalled) return;
			hp = controlHp = 0f;
			deathFunctionCalled = true;
			gameObject.layer = LayerMask.NameToLayer("Default");
			enemyMovement.StopMovement();
			EnemyBehaviour.DeathBehaviour(this);
			StopAllCoroutines();
			deaths++;
			A_EnemyDied?.Invoke(this, dc);
		}

		private void OnPaused(bool paused)
		{
			if (enemyMovement != null && enemyMovement.rb != null) enemyMovement.Pause(paused);
		}

		private void OnDissolveFinished()
		{
			ReleaseToPool();
		}

		public void ReleaseToPoolNextFrame()
		{
			Invoke(nameof(ReleaseToPool), 0f);
		}

		public void ReleaseToPool()
		{
			gameObject.SetActive(false);
			PoolManager.Instance.enemyPool.Release(gameObject);
			ClearAllDebuffs();
			A_EnemyReleasedFromPool?.Invoke(this);
		}

		public void MyFixedUpdate()
		{
			if (!gameObject.activeInHierarchy) return;
			if (allowSpecialAttacks) specialAttackController?.Tick();
			TryTeleport();
			TryDespawn();
			enemyMovement.MyFixedUpdate();
			EnemyBehaviour.FixedUpdate(this);
			VerifyPosition();
			UpdateDamageFlash();
			_toAddBuffer.Clear();
			_toAddBuffer.AddRange(debuffsToAdd.Values);
			foreach (AddDebuffContainer pending in _toAddBuffer) AddDebuffImplementation(pending);
			debuffsToAdd.Clear();
		}

		private void UpdateDamageFlash()
		{
			if (!flashing || MyTime.time <= stopFlashTime) return;
			flashing = false;
			if (hp > 0f && renderer != null && enemyData != null) renderer.sharedMaterial = enemyData.material;
		}

		public void AddDebuff(EDebuff eDebuff, DamageContainer dc, float duration, int stacks = 1)
		{
			if (isInvulnerable) return;
			if (debuffsToAdd.TryGetValue(eDebuff, out AddDebuffContainer pending))
			{
				pending.duration = Mathf.Max(pending.duration, duration);
				pending.stacks += stacks;
				debuffsToAdd[eDebuff] = pending;
			}
			else debuffsToAdd.Add(eDebuff, new AddDebuffContainer(eDebuff, dc, duration, stacks));
		}

		private void AddDebuffImplementation(AddDebuffContainer debuffContainer)
		{
			if ((debuffContainer.eDebuff & EDebuff.DebuffsWithCap) != 0 && debuffCounts.TryGetValue(debuffContainer.eDebuff, out int applications))
			{
				applications++;
				debuffCounts[debuffContainer.eDebuff] = applications;
				if (applications > EnemyStats.GetCapCC()) return;
			}
			if (debuffs.TryGetValue(debuffContainer.eDebuff, out EnemyDebuff existing))
				existing.Refresh(debuffContainer.duration, debuffContainer.stacks);
			else
			{
				EnemyDebuff added = DebuffFactory.GetDebuff(debuffContainer.eDebuff, this, debuffContainer.dc, debuffContainer.duration, debuffContainer.stacks);
				if (added != null) debuffs.Add(debuffContainer.eDebuff, added);
			}
			A_DebuffAdded?.Invoke(debuffContainer.eDebuff);
		}

		public void RemoveDebuff(EDebuff debuff, bool fromDeath)
		{
			if (!debuffs.TryGetValue(debuff, out EnemyDebuff existing)) return;
			existing.OnRemove(fromDeath);
			DebuffFactory.ReturnDebuff(debuff, existing);
			debuffs.Remove(debuff);
			A_DebuffRemoved?.Invoke(debuff);
		}

		public void DebuffTick()
		{
			if (!gameObject.activeInHierarchy) return;
			if (debuffs.Count == 0)
			{
				debuffsToRemove.Clear();
				return;
			}
			foreach (EnemyDebuff debuff in new List<EnemyDebuff>(debuffs.Values))
			{
				debuff.Tick();
				if (!gameObject.activeInHierarchy) return;
				if (debuff.IsDone()) debuffsToRemove.Add(debuff.GetDebuffType());
			}
			foreach (EDebuff debuff in debuffsToRemove) RemoveDebuff(debuff, false);
			debuffsToRemove.Clear();
		}

		public void QueueClearAllDebuffs()
		{
			foreach (EDebuff debuff in debuffs.Keys) debuffsToRemove.Add(debuff);
		}

		public void ClearAllDebuffs()
		{
			foreach (EnemyDebuff debuff in new List<EnemyDebuff>(debuffs.Values))
				RemoveDebuff(debuff.GetDebuffType(), true);
			debuffs.Clear();
			debuffsToAdd.Clear();
			debuffsToRemove.Clear();
		}

		public void Charm(DamageContainer dc, float duration)
		{
			AddDebuff(EDebuff.Charm, dc, duration);
			enemyMovement.state = EnemyMovementRb.State.Charmed;
			if (MyPlayer.Instance != null && MyPlayer.Instance.playerMovement != null &&
				target == MyPlayer.Instance.playerMovement.GetRb())
				target = GameManager.Instance.GetPlayerMovement().GetRb();
			GameObject effect = PoolManager.Instance != null && PoolManager.Instance.charmPool != null
				? PoolManager.Instance.charmPool.Get() : null;
			if (effect != null) effect.transform.position = transform.position + Vector3.up * (meshHeight * .7f) + Vector3.down;
		}

		public void ReleaseCharm()
		{
			enemyMovement.state = EnemyMovementRb.State.Normal;
			target = GameManager.Instance.GetPlayerMovement().GetRb();
		}

		public void FindTarget()
		{
			target = GameManager.Instance.GetPlayerMovement().GetRb();
		}

		private void VerifyPosition()
		{
			if (nextVerifyTime > MyTime.time) return;
			nextVerifyTime = MyTime.time + 5f;
			if (transform.position.y > MapInfo.DespawnEnemyHeight() + 1f) return;
			if ((enemyFlag & EEnemyFlag.AnyBoss) != 0) TeleportToPlayer();
			Invoke(nameof(ReleaseToPool), 0f);
		}

		public void TeleportToPlayer()
		{
			StartCoroutine(StartTeleporting(SpawnPositions.GetEnemySpawnPosition(enemyData, 50, true, float.MaxValue), true));
		}

		private void TryDespawn()
		{
			if (enemyData.despawnTime > 0f && despawnAtTime > 0f && MyTime.time > despawnAtTime && state != EEnemyState.Teleporting)
				StartCoroutine(Despawn());
		}

		public bool IsTeleporting()
		{
			return state == EEnemyState.Teleporting;
		}

		private void TryTeleport()
		{
			if (!CanMove() || nextTeleportTimeCheck > MyTime.time || state == EEnemyState.Teleporting) return;
			nextTeleportTimeCheck = MyTime.time + teleportCheckInterval;
			if (GameManager.Instance.isCrypt || enemyData.teleportCooldown <= 0f || state != EEnemyState.Default ||
				enemyMovement.distanceToTarget < startTeleportThresholdDistance || MyTime.time < lastTeleportTime + enemyData.teleportCooldown) return;
			Vector3 candidate = target.position + (target.position - transform.position);
			if (candidate.x > MapInfo.mapBoundsUpper.x) candidate.x = MapInfo.mapBoundsUpper.x - 1f;
			else if (candidate.x < MapInfo.mapBoundsLower.x) candidate.x = MapInfo.mapBoundsLower.x + 1f;
			if (candidate.z > MapInfo.mapBoundsUpper.z) candidate.z = MapInfo.mapBoundsUpper.z - 1f;
			else if (candidate.z < MapInfo.mapBoundsLower.z) candidate.z = MapInfo.mapBoundsLower.z + 1f;
			RaycastHit[] hits = Physics.RaycastAll(candidate + Vector3.up * 999f, Vector3.down, 9999f, GameManager.Instance.whatIsGroundAndObjects);
			RaycastHit hit = SpawnPositions.FindHitClosestToPlayerY(hits, out bool found, true);
			if (!found) return;
			lastTeleportTime = MyTime.time;
			StartCoroutine(StartTeleporting(hit.point + Vector3.up * (meshHeight * .5f)));
		}

		public void StartSpecialAttack()
		{
			state = EEnemyState.Idle;
			enemyMovement.StopMovement();
		}

		public void EndSpecialAttack()
		{
			state = EEnemyState.Default;
			enemyMovement.StartMovement();
		}

		public void FollowTarget(Transform target)
		{
			followTarget = target;
			state = EEnemyState.FollowTarget;
			enemyMovement.StartMovement();
		}

		public void MyUpdate()
		{
			if (gameObject.activeInHierarchy) enemyMovement.MyUpdate();
		}

		public bool CanMove()
		{
			return hp > 0f && (state == EEnemyState.Default || state == EEnemyState.FollowTarget) &&
				target != null && !rb.isKinematic && !GameManager.Instance.IsTimeFreeze();
		}

		public bool IsRunningFromPlayer()
		{
			if (enemyData.minStayAtDistance > 0f) return minStayAtDistance > enemyMovement.distanceToTarget;
			return enemyData.isRunningFromPlayer && maxHp > hp;
		}

		private bool IsStationary()
		{
			return enemyData.minStayAtDistance > 0f && enemyMovement.distanceToTarget > minStayAtDistance &&
				enemyMovement.distanceToTarget < maxStayAtDistance;
		}

		public int GetXp()
		{
			return IsElite() ? enemyData.GetEliteXp() : enemyData.xp;
		}

		private void ResetMaterial()
		{
			if (hp > 0f) renderer.sharedMaterial = enemyData.material;
		}

		public bool IsDead()
		{
			return hp <= 0f;
		}

		public bool IsDeadOrDyingNextFrame()
		{
			return hp <= 0f || isDyingNextFrame;
		}

		public Vector3 GetCenterPosition()
		{
			return transform.position;
		}

		public Vector3 GetFeetPosition()
		{
			return renderer.transform.position;
		}

		public Vector3 GetHeadPosition()
		{
			return transform.position + Vector3.up * (meshHeight * 0.7f);
		}

		public Vector3 GetGroundCheckPosition()
		{
			return transform.position + Vector3.down * (meshHeight * .5f) + Vector3.up * .25f;
		}

		public bool IsImportantEnemy()
		{
			return IsElite() || IsBoss();
		}

		public float GetHeight()
		{
			return meshHeight;
		}

		public void DisableSpecialAttacks()
		{
			allowSpecialAttacks = false;
		}

		public Vector3 GetBottomPosition()
		{
			return transform.position + Vector3.down * (meshHeight * .5f);
		}

		public float GetPowerupDropChance()
		{
			return basePowerupDropChance;
		}

		public float GetHpRatio()
		{
			return hp / maxHp;
		}
	}
}
