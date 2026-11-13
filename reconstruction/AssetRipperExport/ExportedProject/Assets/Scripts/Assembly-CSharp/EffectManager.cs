using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Managers;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.MapGeneration;
using Actors.Enemies;
using Inventory__Items__Pickups;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDoSpawnRockets_003Ed__93 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public float damage;

		public float procCoefficient;

		public string damageSource;

		public int num;

		private int _003Ci_003E5__2;

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
		public _003CDoSpawnRockets_003Ed__93(int _003C_003E1__state)
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
	private sealed class _003CExploderDeath_003Ed__71 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public EffectManager _003C_003E4__this;

		public Enemy enemy;

		private float _003CexplosionRadius_003E5__2;

		private float _003CinflationTime_003E5__3;

		private float _003Ctimer_003E5__4;

		private float _003CrotationSpeed_003E5__5;

		private Vector3 _003CdefaultScale_003E5__6;

		private Vector3 _003CdesiredScale_003E5__7;

		private float _003CrotationTimer_003E5__8;

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
		public _003CExploderDeath_003Ed__71(int _003C_003E1__state)
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

	public GameObject nukePickup;

	public GameObject magnetPickup;

	public GameObject hastePickup;

	public GameObject ragePickup;

	public GameObject shieldPickup;

	public GameObject stonksPickup;

	public GameObject healthPickup;

	public GameObject playerDamage;

	public GameObject playerLandHard;

	public GameObject smokeHit;

	public GameObject goldSkeletonBreakEffect;

	public GameObject xpSkeletonBreakEffect;

	public GameObject pickupOrbFx;

	public GameObject chestPickup;

	public GameObject chestDiscard;

	public GameObject openChestNormal;

	public GameObject openChestGhost;

	public GameObject wuiFreeChest;

	public GameObject magnetFx;

	public GameObject electricPlugFx;

	public GameObject bananaQuest;

	public GameObject luckTomeQuest;

	public GameObject shotgunQuest;

	public GameObject katanaQuest;

	public GameObject campfire;

	public GameObject banishFx;

	public GameObject stealItemWui;

	public GameObject giveItemWui;

	public GameObject mirrorFx;

	public GameObject zapEffect;

	public GameObject desertStormFx;

	public GameObject tornado;

	public GameObject tumbleweed;

	public GameObject monkeCage;

	public GameObject monkeCageKey;

	public GameObject bushQuest;

	public GameObject banditQuest;

	public GameObject boomboxQuest;

	public GameObject presentQuest;

	public GameObject frogQuest1;

	public GameObject frogQuest2;

	public GameObject frogQuest3;

	public GameObject blindSphere;

	public GameObject floorIsLava;

	public GameObject gloveLightning;

	public GameObject glovePoison;

	public GameObject gloveBlood;

	public GameObject gloveCurse;

	public GameObject glovePower;

	public GameObject[] desertGraves;

	public GameObject lanternExplosion;

	public GameObject enemyHpBar;

	private List<EffectStat> effectStatsQueue;

	public static EffectManager Instance;

	private EffectPlayer playerDamageFx;

	private float nextBloodTimeReady;

	private float bloodCooldown = .1f;

	private string critText = "CRIT!";

	private string megaCritText = "MEGACRIT!";

	public HashSet<Enemy> currentlyExplodingEnemy = new HashSet<Enemy>();

	private AttackHit attackHit;

	private static readonly Dictionary<EWeapon, string> weaponNamesCache = new Dictionary<EWeapon, string>();

	public GameObject americanExplosionEffect;

	public GameObject americanExplosionEffectStart;

	private float baseChestDropChance = 0.0005f;

	private float lastChestAtTime;

	private EffectPlayer electricPlugEffect;

	private EffectPlayer activeMirrorFx;

	private EffectPlayer activeZapFx;

	private DesertStorm desertStorm;

	public GameObject spawnedLuckTomeObject;

	public GameObject spawnedShotgunObject;

	public GameObject spawnedKatanaObject;

	private Dictionary<GameObject, ItemProjectile> activeGhostProjectiles = new Dictionary<GameObject, ItemProjectile>();

	private bool hasSpawnedFirstEliteChest;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		Pickup.A_PickupTriggered += OnPickup;
		PlayerHealth.A_Heal += OnHeal;
		PlayerHealth.A_TakeDamage += OnDamage;
		Enemy.A_Damage += OnEnemyDamage;
		Enemy.A_EnemyDied += OnEnemyDied;
		ItemInventory.A_ItemAdded += OnItemAdded;
		ItemInventory.A_ItemRemoved += OnItemRemoved;
		MapGenerationController.A_GenerationComplete += OnMapGenerationComplete;
	}

	public void PlayerLandHard()
	{
		if (GameManager.Instance == null || PlayerMovement.Instance == null || playerLandHard == null) return;
		Instantiate(playerLandHard, PlayerMovement.Instance.GetRbFeetPosition(), Quaternion.identity);
	}

	private void OnDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
	{
		if (dc == null) return;
		Color color = shieldDamage ? new Color(0f, 1f, 1f, 1f) : new Color(1f, 1f, 0f, 1f);
		if (dc.element == Assets.Scripts.Game.Combat.EElement.Bleed) color = MyColorUtility.bleedColor;
		if (dc.damageEffect == Assets.Scripts.Game.Combat.EDamageEffect.Poison) color = MyColorUtility.poisonColor;
		Vector3 direction = -dc.direction;
		if (direction.sqrMagnitude < 1e-10f) direction = Vector3.forward;
		var player = GameManager.Instance != null ? GameManager.Instance.GetPlayer() : null;
		if (playerDamageFx == null && playerDamage != null && player != null)
		{
			GameObject effect = Instantiate(playerDamage, player.transform.position, Quaternion.LookRotation(direction));
			if (effect != null) playerDamageFx = effect.GetComponent<EffectPlayer>();
		}
		if (MyTime.time >= nextBloodTimeReady && playerDamageFx != null && MyPlayer.Instance != null)
		{
			nextBloodTimeReady = MyTime.time + bloodCooldown;
			playerDamageFx.transform.position = MyPlayer.Instance.transform.position;
			playerDamageFx.transform.rotation = Quaternion.LookRotation(direction);
			playerDamageFx.Play();
		}
		if (SaveManager.Instance?.config?.cfVisualsSettings != null && SaveManager.Instance.config.cfVisualsSettings.damage_numbers == 1 && MyPlayer.Instance != null && PoolManager.Instance != null)
			PopupText("-" + ((int)dc.damage).ToString(), color, MyPlayer.Instance.transform.position + Vector3.up, 20);
	}

	public void NewDamageNumbers(DamageContainer dc, Enemy enemy)
	{
		if (SaveManager.Instance == null || SaveManager.Instance.config == null || SaveManager.Instance.config.cfVisualsSettings.damage_numbers == 0) return;
		PopupText(dc.damage, dc.GetColor(), enemy.transform.position + Vector3.up, 24);
		if (dc.damageEffect == Assets.Scripts.Game.Combat.EDamageEffect.Megacrit)
			PopupText(megaCritText, MyColorUtility.critMegaColor, enemy.transform.position + Vector3.up * 2f, 20);
		else if (dc.crit)
			PopupText(critText, new Color(1f, 235f / 255f, 4f / 255f, 1f), enemy.transform.position + Vector3.up * 2f, 16);
	}

	public void PopupText(string text, Color color, Vector3 position, int textSize = 24)
	{
		DamageNumbers number = PoolManager.Instance.GetDamageNumber();
		if (number != null) number.SetDamage(text, color, position, textSize);
	}

	public void PopupText(float damage, Color color, Vector3 position, int textSize = 24)
	{
		DamageNumbers number = PoolManager.Instance.GetDamageNumber();
		if (number != null) number.SetDamage(damage, color, position, textSize);
	}

	public void PickupEffect()
	{
		GameObject effect = PoolManager.Instance.pickupeffectPool.Get();
		if (effect == null) return;
		effect.SetActive(true);
		effect.transform.position = GameManager.Instance.GetPlayer().transform.position;
	}

	public void GoldBurstEffect(Vector3 position)
	{
		GameObject effect = PoolManager.Instance.goldBurstPool.Get();
		if (effect == null) return;
		effect.SetActive(true);
		effect.transform.position = position;
	}

	private void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
	{
		if (enemy == null) return;
		CheckChestSpawn(enemy);
		var player = MyPlayer.Instance;
		if (player == null || player.inventory == null) return;
		if (player.inventory.statusEffects != null && player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Stonks) && PoolManager.Instance != null)
			GoldBurstEffect(enemy.transform.position);
		if (enemy.enemyData == null) return;
		if (enemy.enemyData.enemyName == EEnemy.GoldenSkeleton)
		{
			if (goldSkeletonBreakEffect != null) Instantiate(goldSkeletonBreakEffect, enemy.transform.position, Quaternion.identity);
			MoneyUtility.SpawnMoney(MoneyUtility.GetChestPrice() / 2, enemy.transform.position);
		}
		else if (enemy.enemyData.enemyName == EEnemy.XpSkeleton && player.inventory.playerXp != null && PickupManager.Instance != null)
		{
			int xp = XpUtility.XpToNextLevelTotal(player.inventory.playerXp.xp);
			int count = Mathf.Min(xp, 10);
			if (xpSkeletonBreakEffect != null) Instantiate(xpSkeletonBreakEffect, enemy.transform.position, Quaternion.identity);
			if (count <= 0) return;
			int value = Mathf.CeilToInt((float)xp / count);
			for (int i = 0; i < count; i++)
			{
				Pickup pickup = PickupManager.Instance.SpawnPickup(EPickup.Xp, enemy.transform.position + Vector3.up, value, true, 0.25f);
				if (pickup != null) pickup.StartFollowingPlayer(player.transform);
			}
		}
	}

	private void OnEnemyDamage(Enemy enemy, DamageContainer dc)
	{
		if (PoolManager.Instance == null) return;
		var effect = dc.damageEffect;
		UnityEngine.Pool.ObjectPool<GameObject> pool;
		if (effect == Assets.Scripts.Game.Combat.EDamageEffect.Execute) pool = PoolManager.Instance.executePool;
		else if (effect == Assets.Scripts.Game.Combat.EDamageEffect.Bloodmark) pool = PoolManager.Instance.bloodmarkPool;
		else if (effect == Assets.Scripts.Game.Combat.EDamageEffect.Megacrit) pool = PoolManager.Instance.megaCritPool;
		else if (dc.crit) pool = PoolManager.Instance.critEffectPool;
		else return;
		if (pool == null) return;
		GameObject obj = pool.Get();
		if (obj == null) return;
		obj.SetActive(true);
		if (effect == Assets.Scripts.Game.Combat.EDamageEffect.Execute) obj.transform.position = enemy.transform.position + Vector3.up * 3f;
		else if (effect == Assets.Scripts.Game.Combat.EDamageEffect.Bloodmark) obj.transform.position = enemy.GetCenterPosition();
		else
		{
			Vector3 hit = enemy.collider.ClosestPoint(MyPlayer.Instance.transform.position);
			obj.transform.position = Vector3.Lerp(hit, enemy.transform.position, .5f);
		}
	}

	public void ExploderEnemy(Enemy enemy)
	{
		StartCoroutine(ExploderDeath(enemy));
	}

	private IEnumerator ExploderDeath(Enemy enemy)
	{
		currentlyExplodingEnemy.Add(enemy);
		enemy.MakeWhite();
		Instantiate(americanExplosionEffectStart, enemy.GetFeetPosition(), Quaternion.identity);
		WarningSphere(enemy.GetFeetPosition(), 8f, 0.75f, null);
		Vector3 originalScale = enemy.transform.localScale;
		float progress = 0f, spinTimer = 0f;
		float spin = UnityEngine.Random.Range(0, 2) == 0 ? 400f : -400f;
		while (progress < 1f && enemy != null && enemy.gameObject.activeInHierarchy)
		{
			if (MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze)) { yield return null; continue; }
			progress += MyTime.deltaTime / 0.75f;
			spinTimer += MyTime.deltaTime;
			if (spinTimer > 0.1f) { enemy.transform.Rotate(Vector3.up, spin * spinTimer); spinTimer = 0f; }
			enemy.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, progress);
			yield return null;
		}
		if (enemy != null && enemy.gameObject.activeInHierarchy)
		{
			GameObject effect = Instantiate(americanExplosionEffect, enemy.transform.position, Quaternion.identity);
			CombatExplosion explosion = effect.GetComponent<CombatExplosion>();
			explosion.radius = 8f;
			explosion.playerDamage = enemy.enemyData.GetDamage();
			enemy.transform.localScale = originalScale;
			enemy.ReleaseToPool();
		}
		currentlyExplodingEnemy.Remove(enemy);
	}

	public void OnHeal(PlayerHealth ph, float value, bool isShield)
	{
		if (isShield || MyPlayer.Instance == null || PoolManager.Instance == null) return;
		PopupText("+" + ((int)value).ToString(), new Color(0f, 1f, 0f, 1f), MyPlayer.Instance.transform.position + Vector3.up, 18);
	}

	public void EnemyHitEffect(Vector3 hitPos, Vector3 moveDir, bool hitEnemy, string source, GameObject weaponHitEffect, bool useSfx)
	{
		attackHit = PoolManager.Instance.GetProjectileHit(source, weaponHitEffect);
		if (attackHit == null) return;
		attackHit.transform.position = hitPos;
		attackHit.transform.rotation = moveDir.sqrMagnitude < 1e-10f ? Quaternion.identity : Quaternion.LookRotation(-moveDir);
		attackHit.pool = PoolManager.Instance.projectileHitPools[source];
		attackHit.Play(hitEnemy, useSfx);
	}

	public void EnemyHitEffect(Vector3 hitPos, Vector3 moveDir, bool hitEnemy, EWeapon eWeapon, GameObject weaponHitEffect, bool useSfx)
	{
		if (!weaponNamesCache.TryGetValue(eWeapon, out string source)) weaponNamesCache[eWeapon] = source = eWeapon.ToString();
		EnemyHitEffect(hitPos, moveDir, hitEnemy, source, weaponHitEffect, useSfx);
	}

	public void SpawnPickupOrb(EPickup ePickup, Vector3 position)
	{
		if (pickupOrbFx == null) return;
		GameObject obj = Instantiate(pickupOrbFx, position, Quaternion.identity);
		PickupOrb orb = obj.GetComponent<PickupOrb>();
		if (orb != null)
		{
			orb.Set(ePickup);
			if (PickupManager.Instance != null) PickupManager.Instance.CountAdd();
		}
	}

	public CircleWarning WarningSphere(Vector3 position, float radius, float time, Action completeAction)
	{
		GameObject obj = PoolManager.Instance.warningSpherePool.Get();
		CircleWarning warning = obj.GetComponent<CircleWarning>();
		warning.Set(radius, time, completeAction);
		obj.transform.position = position;
		return warning;
	}

	public bool WarningTube(Vector3 position, Vector3 dir, float radius, float distance, float time, Action completeAction)
	{
		GameObject obj = PoolManager.Instance.warningTubePool.Get();
		if (obj == null) return false;
		obj.GetComponent<TubeWarning>().Set(radius, distance, time, completeAction);
		obj.transform.position = position;
		obj.transform.rotation = Quaternion.LookRotation(dir);
		return true;
	}

	private void CheckChestSpawn(Enemy enemy)
	{
		if (enemy == null) return;
		bool guaranteedBoss = enemy.IsBoss() && !MapController.IsFinalBossStage();
		bool guaranteedElite = !hasSpawnedFirstEliteChest && enemy.IsElite() && !enemy.IsChallenge() && MapController.IsFirstStage();
		if (guaranteedElite && !guaranteedBoss) hasSpawnedFirstEliteChest = true;
		if (!guaranteedBoss && !guaranteedElite)
		{
			if (global::Utility.MyRandom.random.NextDouble() >= GetChestDropChance(enemy)) return;
			lastChestAtTime = MyTime.time;
		}
		if (openChestNormal != null && MyPlayer.Instance != null)
			SpawnChest(openChestNormal, enemy.GetHeadPosition() + Vector3.up * 2f);
	}

	public void SpawnChest(GameObject chestPrefab, Vector3 pos)
	{
		if (ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS)) return;
		Vector3 direction = MyPlayer.Instance.transform.position - pos;
		direction.y = 0f;
		Quaternion rotation = Quaternion.LookRotation(direction);
		Instantiate(chestPrefab, Utility.RaycastUtility.RayToGround(pos), rotation);
	}

	public void SpawnChestForcePosition(GameObject chestPrefab, Vector3 pos)
	{
		if (Assets.Scripts.Saves___Serialization.Progression.Challenges.ChallengesTracker.HasChallengeModifier("no_items")) return;
		Vector3 direction = MyPlayer.Instance.transform.position - pos;
		direction.y = 0f;
		Instantiate(chestPrefab, pos, Quaternion.LookRotation(direction));
	}

	private float GetChestDropChance(Enemy enemy)
	{
		return baseChestDropChance * PlayerStats.GetStat(EStat.PowerupChance) * GetChestDropTimeMultiplier() * (enemy.IsElite() ? 2f : 1f);
	}

	private float GetChestDropTimeMultiplier()
	{
		return Mathf.Clamp01((MyTime.time - lastChestAtTime) / 420f) * 2f;
	}

	private void OnItemRemoved(EItem item, bool showEffect)
	{
		if (showEffect) ShowItemChange(item, -1);
	}

	private void OnItemAdded(EItem item)
	{
		ShowItemChange(item, 1);
	}

	private void ShowItemChange(EItem item, int amount)
	{
		if (MyPlayer.Instance == null || DataManager.Instance == null || chestPickup == null) return;
		ItemData data = DataManager.Instance.GetItem(item);
		var player = MyPlayer.Instance.transform;
		GameObject obj = Instantiate(chestPickup, player.position + player.up * 1.5f, Quaternion.identity);
		ChestItem display = obj.GetComponent<ChestItem>();
		if (display != null) display.Set(data, amount);
	}

	public void BanishItem(UnlockableBase unlockable)
	{
		var player = Assets.Scripts.Actors.Player.MyPlayer.Instance.transform;
		var effect = Instantiate(banishFx, player.position + player.up * 1.5f, Quaternion.identity);
		effect.GetComponent<BanishItem>().Set(unlockable);
	}

	public void SpawnRockets(int num, float damage, float procCoefficient, string damageSource)
	{
		StartCoroutine(DoSpawnRockets(num, damage, procCoefficient, damageSource));
	}

	private IEnumerator DoSpawnRockets(int num, float damage, float procCoefficient, string damageSource)
	{
		for (int i = 0; i < num; i++)
		{
			while (MyTime.paused) yield return null;
			if (PoolManager.Instance == null || PoolManager.Instance.rocketPool == null || MyPlayer.Instance == null) yield break;
			GameObject obj = PoolManager.Instance.rocketPool.Get();
			if (obj == null) yield break;
			Rocket projectile = obj.GetComponent<Rocket>();
			if (projectile == null) { PoolManager.Instance.rocketPool.Release(obj); yield break; }
			projectile.Set(MyPlayer.Instance.transform.position + Vector3.up * 2f, damage, procCoefficient, null, true, damageSource);
			yield return new WaitForSeconds(0.07f * (1f + (float)global::Utility.MyRandom.random.NextDouble()));
		}
	}

	public void MagnetEffect()
	{
		if (magnetFx == null || MyPlayer.Instance == null) return;
		Transform player = MyPlayer.Instance.transform;
		GameObject effect = Instantiate(magnetFx, player.position, Quaternion.identity);
		effect.transform.SetParent(player);
	}

	public void ElectricalPlugEffect()
	{
		if (electricPlugEffect == null)
		{
			electricPlugEffect = Instantiate(electricPlugFx, MyPlayer.Instance.transform).GetComponent<EffectPlayer>();
			electricPlugEffect.transform.localPosition = Vector3.zero;
		}
		else electricPlugEffect.Play();
	}

	public void SpawnMirrorFx(Vector3 pos, Vector3 dir, float size)
	{
		if (activeMirrorFx == null) activeMirrorFx = Instantiate(mirrorFx, pos, Quaternion.LookRotation(dir)).GetComponent<EffectPlayer>();
		else activeMirrorFx.Play();
		activeMirrorFx.transform.position = pos;
		activeMirrorFx.transform.rotation = Quaternion.LookRotation(dir);
		activeMirrorFx.transform.localScale = Vector3.one * size;
	}

	public void ZapEffect(Vector3 pos)
	{
		if (activeZapFx == null) activeZapFx = Instantiate(zapEffect, pos, Quaternion.identity).GetComponent<EffectPlayer>();
		else activeZapFx.Play();
		activeZapFx.transform.position = pos;
	}

	public DesertStorm GetDesertStorm()
	{
		if (desertStorm == null)
		{
			GameObject instance = Instantiate(desertStormFx);
			instance.SetActive(false);
			desertStorm = instance.GetComponent<DesertStorm>();
		}
		return desertStorm;
	}

	public void SpawnTornadoes(int amount)
	{
		for (int i = 0; i < amount; i++) Instantiate(tornado);
	}

	public void SpawnTumbleWeeds(int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			GameObject obj = PoolManager.Instance.tumbleweedPool.Get();
			if (obj != null) obj.SetActive(true);
		}
	}

	public void TrySpawnLuckQuest(Vector3 pos)
	{
		if (spawnedLuckTomeObject == null && luckTomeQuest != null)
			spawnedLuckTomeObject = Instantiate(luckTomeQuest, pos, luckTomeQuest.transform.rotation);
	}

	public void TrySpawnShotgunQuest(Vector3 pos)
	{
		if (spawnedShotgunObject == null && shotgunQuest != null)
			spawnedShotgunObject = Instantiate(shotgunQuest, pos, shotgunQuest.transform.rotation);
	}

	public void TrySpawnKatanaQuest(Vector3 pos)
	{
		if (spawnedKatanaObject == null && katanaQuest != null)
			spawnedKatanaObject = Instantiate(katanaQuest, pos, katanaQuest.transform.rotation);
	}

	public void TakeItem(UnlockableBase data, Transform target, Vector3 targetOffset, float hoverTime = 1f, float moveTime = 1f, float scale = 1f)
	{
		Instantiate(stealItemWui).GetComponent<StealWeaponWui>().Set(data, target, targetOffset, hoverTime, moveTime, scale);
	}

	public void GiveItem(UnlockableBase data)
	{
		Instantiate(giveItemWui).GetComponent<ReturnWeaponWui>().Set(data);
	}

	private void OnMapGenerationComplete()
	{
		if (MapController.currentMap == null || GameManager.Instance == null) return;
		if (MapController.currentMap.eMap == EMap.Forest && SaveManager.Instance != null &&
			SaveManager.Instance.progression != null && !MyAchievements.IsAchievementDone("a_monke") &&
			monkeCageKey != null && monkeCage != null)
		{
			Vector3 keyPosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, .5f,
				GameManager.Instance.whatIsGroundAndObjects, out _, 100, false, false, false, 44f, 0f);
			Vector3 cagePosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 4f,
				GameManager.Instance.whatIsGroundAndObjects, out _, 100, false, false, false, 1f, 0f);
			Instantiate(monkeCageKey, keyPosition + Vector3.up * 2f, monkeCageKey.transform.rotation);
			GameObject cage = Instantiate(monkeCage, cagePosition, monkeCage.transform.rotation);
			cage.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.World);
			Vector3 toCenter = MapInfo.mapCenter - cagePosition;
			toCenter.y = 0f;
			cage.transform.rotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
		}
		if (MapController.currentMap.eMap == EMap.Forest && bushQuest != null)
		{
			Vector3 bushPosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 2f,
				GameManager.Instance.whatIsGroundAndObjects, out _, 100, false, false, false, 1f, 0f);
			GameObject bush = Instantiate(bushQuest, bushPosition, bushQuest.transform.rotation);
			bush.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f));
		}
		if (MapController.currentMap.eMap == EMap.Desert && banditQuest != null)
		{
			Vector3 banditPosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 3f,
				GameManager.Instance.whatIsGroundAndObjects, out Vector3 normal, 100, false, false, false, 15f, 0f);
			GameObject bandit = Instantiate(banditQuest, banditPosition, banditQuest.transform.rotation);
			bandit.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
		}
		if (MapController.currentMap.eMap == EMap.Desert && CharacterMenu.selectedCharacter == ECharacter.Calcium &&
			MapController.runConfig != null && MapController.runConfig.mapTierIndex > 0 && desertGraves != null)
		{
			var graves = new List<GameObject>();
			foreach (GameObject prefab in desertGraves)
			{
				if (prefab == null) continue;
				Vector3 gravePosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 2f,
					GameManager.Instance.whatIsGroundAndObjects, out _, 200, true, false, false, 40f, 0f);
				GameObject grave = Instantiate(prefab, gravePosition, prefab.transform.rotation);
				grave.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.World);
				graves.Add(grave);
				if (graves.Count > 1) grave.SetActive(false);
			}
			for (int i = 0; i + 1 < graves.Count; i++)
			{
				InteractableDesertGrave current = graves[i].GetComponent<InteractableDesertGrave>();
				if (current != null) current.nextShrine = graves[i + 1].GetComponent<ShrineSpawnAnimation>();
			}
		}
		if (MapController.currentMap.eMap != EMap.Forest) return;
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null && DataManager.Instance != null &&
			boomboxQuest != null && !MyAchievements.IsAchievementDone("a_boombox"))
		{
			int count = MyAchievements.GetAchievementTargetValue("a_boombox");
			for (int i = 0; i < count; i++)
			{
				Vector3 boomboxPosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 2f,
					GameManager.Instance.whatIsGroundAndObjects, out _, 100, false, false, false, 40f, 0f);
				GameObject boombox = Instantiate(boomboxQuest, boomboxPosition, boomboxQuest.transform.rotation);
				boombox.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.World);
			}
		}
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null &&
			!MyAchievements.IsAchievementDone("a_santaHat") && presentQuest != null)
		{
			Vector3 giftPosition = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 3f,
				GameManager.Instance.whatIsGroundAndObjects, out Vector3 normal, 100, false, false, false, 15f, 0f);
			Quaternion rotation = banditQuest != null ? banditQuest.transform.rotation : presentQuest.transform.rotation;
			GameObject gift = Instantiate(presentQuest, giftPosition, rotation);
			gift.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
		}
		GameObject frog = MapController.index == 0 ? frogQuest1 : MapController.index == 1 ? frogQuest2 : MapController.index == 2 ? frogQuest3 : null;
		Vector3 position = SpawnPositions.GetObjectSpawnPosition(MapInfo.mapCenter, MapInfo.mapSize, 2f,
			GameManager.Instance.whatIsGroundAndObjects, out _, 100, false, false, false, 1f, 0f);
		if (frog != null) Instantiate(frog, position, frog.transform.rotation);
	}

	public void SpawnGhostProjectile(float damage, float duration, string damageSource)
	{
		var pool = PoolManager.Instance != null ? PoolManager.Instance.ghostPool : null;
		if (pool == null || MyPlayer.Instance == null) return;
		GameObject obj = pool.Get();
		if (obj == null)
		{
			if (activeGhostProjectiles.Count == 0) return;
			int index = global::Utility.MyRandom.random.Next(activeGhostProjectiles.Count);
			foreach (var pair in activeGhostProjectiles)
				if (index-- == 0) { if (pair.Value != null) pair.Value.AddDamage(damage); return; }
			return;
		}
		if (!activeGhostProjectiles.TryGetValue(obj, out ItemProjectile projectile))
			activeGhostProjectiles[obj] = projectile = obj.GetComponent<ItemProjectile>();
		if (projectile == null) { pool.Release(obj); return; }
		obj.SetActive(true);
		projectile.Set(MyPlayer.Instance.transform.position, damage, 1f, damageSource, pool, 0, 1, duration, 30f);
	}

	public void AttachEnemyHpBar(Enemy enemy)
	{
		Instantiate(enemyHpBar).GetComponent<EnemyHpBar>().Set(enemy);
	}

	private void OnDestroy()
	{
		Pickup.A_PickupTriggered -= OnPickup;
		PlayerHealth.A_Heal -= OnHeal;
		PlayerHealth.A_TakeDamage -= OnDamage;
		Enemy.A_Damage -= OnEnemyDamage;
		Enemy.A_EnemyDied -= OnEnemyDied;
		ItemInventory.A_ItemAdded -= OnItemAdded;
		ItemInventory.A_ItemRemoved -= OnItemRemoved;
		MapGenerationController.A_GenerationComplete -= OnMapGenerationComplete;
		if (Instance == this) Instance = null;
	}

	private void OnPickup(Pickup pickup)
	{
		GameObject prefab;
		bool statusEffect = false;
		switch (pickup.ePickup)
		{
			case EPickup.Health: prefab = healthPickup; break;
			case EPickup.Nuke: prefab = nukePickup; break;
			case EPickup.Magnet: prefab = magnetPickup; break;
			case EPickup.Haste: prefab = hastePickup; statusEffect = true; break;
			case EPickup.Rage: prefab = ragePickup; statusEffect = true; break;
			case EPickup.Shield: prefab = shieldPickup; statusEffect = true; break;
			case EPickup.Stonks: prefab = stonksPickup; break;
			default: return;
		}
		if (prefab == null) return;
		GameObject effect = Instantiate(prefab, pickup.transform.position, Quaternion.identity);
		if (statusEffect) effect.GetComponent<StatusEffectPickup>()?.Set();
	}
}
