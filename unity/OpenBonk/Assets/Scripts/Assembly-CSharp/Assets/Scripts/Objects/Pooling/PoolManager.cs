using System.Collections.Generic;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.Objects.Pooling
{
	public class PoolManager : MonoBehaviour
	{
		public Transform damageNumbersParent;

		private static int maxCritPrefabs = 20;

		private int maxBonkPrefabs = 10;

		private int maxBulletHitPrefabs = 20;

		private int maxHitNumbers = 100;

		private int maxLightningStrikes = 70;

		private int maxFirefields = 50;

		public const int rocketPoolSize = 200;

		private int orbPoolSize = 200;

		private int explosionPoolSize = 100;

		private static int maxSilver = 30;

		private static int cursedHitsPoolSize = 20;

		private static int charmPoolSize = 20;

		private static int eatPoolSize = 10;

		public static PoolManager Instance;

		private int maxWarningSpheres = 5000;

		public GameObject xpPrefab;

		public GameObject goldPrefab;

		public GameObject silverPrefab;

		public GameObject enemyPrefab;

		public GameObject enemySpawnFxPrefab;

		public GameObject damageNumbersPrefab;

		public GameObject pickupEffectPrefab;

		public GameObject goldBurstPrefab;

		public GameObject critEffectPrefab;

		public GameObject lightningStrikePrefab;

		public GameObject footstepPrefab;

		public GameObject enemyStatusSymbolsPrefab;

		public ObjectPool<GameObject> xpPool;

		public ObjectPool<GameObject> goldPool;

		public ObjectPool<GameObject> silverPool;

		public ObjectPool<GameObject> enemyPool;

		public ObjectPool<GameObject> enemySpawnFxPool;

		public ObjectPool<GameObject> damageNumbersPool;

		public ObjectPool<GameObject> pickupeffectPool;

		public ObjectPool<GameObject> goldBurstPool;

		public ObjectPool<GameObject> critEffectPool;

		public ObjectPool<GameObject> lightningStrikePool;

		public ObjectPool<GameObject> footstepPool;

		public ObjectPool<GameObject> enemyStatusSymbolsPool;

		public ObjectPool<GameObject> powerupPool;

		public ObjectPool<GameObject> warningTubePool;

		public ObjectPool<GameObject> warningSpherePool;

		public ObjectPool<GameObject> bullseyePool;

		public ObjectPool<GameObject> poisonPool;

		public GameObject defaultHitPrefab;

		public GameObject powerupPrefab;

		public GameObject warningTube;

		public GameObject warningSphere;

		public GameObject bullseye;

		public GameObject poisonCloud;

		public Dictionary<EWeapon, ObjectPool<GameObject>> weaponAttackPools = new Dictionary<EWeapon, ObjectPool<GameObject>>();

		public Dictionary<EWeapon, ObjectPool<GameObject>> projectilePools = new Dictionary<EWeapon, ObjectPool<GameObject>>();

		public Dictionary<string, ObjectPool<GameObject>> projectileDonePools;

		public Dictionary<string, ObjectPool<GameObject>> projectileHitPools;

		public Dictionary<string, ObjectPool<GameObject>> enemyAttacksPools;

		public Dictionary<EEnemyAttack, ObjectPool<GameObject>> enemyAttacksFxPools;

		private int itemFxPoolSizes = 20;

		public ObjectPool<GameObject> grandmaTonicPool;

		public ObjectPool<GameObject> megaCritPool;

		public ObjectPool<GameObject> executePool;

		public ObjectPool<GameObject> spicyMeatballPool;

		public ObjectPool<GameObject> chainLightningPool;

		public ObjectPool<GameObject> freezeFxPool;

		public ObjectPool<GameObject> firefieldPool;

		public ObjectPool<GameObject> rocketPool;

		public ObjectPool<GameObject> explosionPool;

		public ObjectPool<GameObject> bonkPool;

		public ObjectPool<GameObject> bulletHitPool;

		public ObjectPool<GameObject> cursedHitPool;

		public ObjectPool<GameObject> orbPool;

		public ObjectPool<GameObject> ghostPool;

		public ObjectPool<GameObject> angrySoulPool;

		public ObjectPool<GameObject> charmPool;

		public ObjectPool<GameObject> borgorPool;

		public ObjectPool<GameObject> eatPool;

		public ObjectPool<GameObject> bloodmarkPool;

		public ObjectPool<GameObject> cactusPool;

		public ObjectPool<GameObject> tumbleweedPool;

		public ObjectPool<GameObject> tumbleweedBreakPool;

		public ObjectPool<GameObject> quinMaskPool;

		public ObjectPool<GameObject> snekPool;

		public GameObject grandmaTonicFx;

		public GameObject megaCritFx;

		public GameObject spicyMeatballFx;

		public GameObject chainLightningFx;

		public GameObject freezeFx;

		public GameObject firefield;

		public GameObject rocket;

		public GameObject explosionFx;

		public GameObject bonkFx;

		public GameObject executeFx;

		public GameObject bulletHit;

		public GameObject cursedHit;

		public GameObject orb;

		public GameObject ghost;

		public GameObject angrySoul;

		public GameObject charmFx;

		public GameObject borgor;

		public GameObject eat;

		public GameObject bloodmark;

		public GameObject cactusFx;

		public GameObject tumbleweed;

		public GameObject tumbleweedBreak;

		public GameObject quinMaskFx;

		public GameObject snekFx;

		private Dictionary<GameObject, WeaponAttack> weaponAttacksDict = new Dictionary<GameObject, WeaponAttack>();

		private Dictionary<GameObject, ProjectileBase> projectileDict = new Dictionary<GameObject, ProjectileBase>();

		private Dictionary<GameObject, DamageNumbers> damageNumbersDict = new Dictionary<GameObject, DamageNumbers>();

		private Dictionary<GameObject, AttackHit> attackHitsDict;

		private Dictionary<EWeapon, string> weaponStrings;

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
		}

		private void Start()
		{
			if (footstepPrefab != null) footstepPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(footstepPrefab, footstepPool, 10, .6f), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 5, 10);
			if (bulletHit != null) bulletHitPool = CreateObjectPoolTimeout(bulletHit, maxBulletHitPrefabs, 1.5f);
			if (ghost != null) ghostPool = new ObjectPool<GameObject>(() => CreatePooledItem(ghost, ghostPool, 180), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, 180);
			if (angrySoul != null) angrySoulPool = new ObjectPool<GameObject>(() => CreatePooledItem(angrySoul, angrySoulPool, 100), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, 100);
			if (rocket != null) rocketPool = new ObjectPool<GameObject>(() => CreatePooledItem(rocket, rocketPool, rocketPoolSize), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, rocketPoolSize);
			if (bullseye != null) bullseyePool = new ObjectPool<GameObject>(() => CreatePooledItem(bullseye, bullseyePool, 20), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 5, 20);
			if (explosionFx != null) explosionPool = CreateObjectPoolTimeout(explosionFx, explosionPoolSize, 1f);
			InitializePoisonPool();
			if (critEffectPrefab != null) critEffectPool = CreateObjectPoolTimeout(critEffectPrefab, maxCritPrefabs, .5f);
			if (megaCritFx != null) megaCritPool = CreateObjectPoolTimeout(megaCritFx, itemFxPoolSizes, .5f);
			if (executeFx != null) executePool = CreateObjectPoolTimeout(executeFx, itemFxPoolSizes, .5f);
			if (grandmaTonicFx != null) grandmaTonicPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(grandmaTonicFx, grandmaTonicPool, itemFxPoolSizes, .5f), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, itemFxPoolSizes, itemFxPoolSizes);
			if (quinMaskFx != null) quinMaskPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(quinMaskFx, quinMaskPool, itemFxPoolSizes, .5f), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, itemFxPoolSizes, itemFxPoolSizes);
			if (spicyMeatballFx != null) spicyMeatballPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(spicyMeatballFx, spicyMeatballPool, itemFxPoolSizes, .5f), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, itemFxPoolSizes, itemFxPoolSizes);
			if (bonkFx != null) bonkPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(bonkFx, bonkPool, maxBonkPrefabs, 1f), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, maxBonkPrefabs);
			if (snekFx != null) snekPool = CreateObjectPoolTimeout(snekFx, itemFxPoolSizes, .8f);
			if (orb != null) orbPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(orb, orbPool, orbPoolSize),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, orbPoolSize);
			if (bloodmark != null) bloodmarkPool = CreateObjectPoolTimeout(bloodmark, maxBonkPrefabs, .7f);
			if (eat != null) eatPool = CreateObjectPoolTimeout(eat, eatPoolSize, .7f);
			if (cursedHit != null) cursedHitPool = CreateObjectPoolTimeout(cursedHit, cursedHitsPoolSize, .5f);
			if (charmFx != null) charmPool = CreateObjectPoolTimeout(charmFx, charmPoolSize, .5f);
			if (cactusFx != null) cactusPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(cactusFx, cactusPool, maxFirefields, .35f), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, maxFirefields, maxFirefields);
			damageNumbersPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(damageNumbersPrefab, damageNumbersPool, maxHitNumbers),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 50, maxHitNumbers);
			// The original Start also creates many effect and pickup pools; those
			// are still being recovered. The enemy pool uses a capacity of ten and
			// the manager's pooled-enemy limit as its maximum.
			xpPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(xpPrefab, xpPool, PickupManager.maxXpObjects),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, PickupManager.maxXpObjects);
			goldPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(goldPrefab, goldPool, MoneyFlying.maxMoneyObjects),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, MoneyFlying.maxMoneyObjects);
			silverPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(silverPrefab, silverPool, maxSilver),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, maxSilver);
			pickupeffectPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(pickupEffectPrefab, pickupeffectPool, 5),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 5, 5);
			goldBurstPool = new ObjectPool<GameObject>(
				() => CreatePooledItemTimeout(goldBurstPrefab, goldBurstPool, 10, 1f),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, 10);
			powerupPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(powerupPrefab, powerupPool, PickupManager.maxPowerupsOnMap),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, PickupManager.maxPowerupsOnMap);
			borgorPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(borgor, borgorPool, 20),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, 20);
			tumbleweedPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(tumbleweed, tumbleweedPool, InteractableTumbleWeed.maxTumbleWeeds),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, InteractableTumbleWeed.maxTumbleWeeds);
			tumbleweedBreakPool = CreateObjectPool(tumbleweedBreak, maxBonkPrefabs);
			firefieldPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(firefield, firefieldPool, maxFirefields),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, maxFirefields);
			lightningStrikePool = CreateObjectPoolTimeout(lightningStrikePrefab, maxLightningStrikes, 0.4f);
			chainLightningPool = CreateObjectPoolTimeout(chainLightningFx, maxLightningStrikes, 0.18f);
			freezeFxPool = CreateObjectPoolTimeout(freezeFx, 20, 1f);
			warningSpherePool = CreateObjectPool(warningSphere, maxWarningSpheres);
			warningTubePool = CreateObjectPool(warningTube, maxWarningSpheres);
			enemyPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(enemyPrefab, enemyPool, EnemyManager.maxNumEnemiesPooled),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10,
				EnemyManager.maxNumEnemiesPooled);
			enemySpawnFxPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(enemySpawnFxPrefab, enemySpawnFxPool, 20),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, 20);
			enemyStatusSymbolsPool = new ObjectPool<GameObject>(
				() => CreatePooledItem(enemyStatusSymbolsPrefab, enemyStatusSymbolsPool, 100),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, 100);
		}

		public DamageNumbers GetDamageNumber()
		{
			if (damageNumbersPool == null) return null;
			GameObject obj = damageNumbersPool.Get();
			if (obj == null) return null;
			if (obj.transform.parent == null) obj.transform.SetParent(damageNumbersParent);
			if (!damageNumbersDict.TryGetValue(obj, out DamageNumbers number)) damageNumbersDict[obj] = number = obj.GetComponent<DamageNumbers>();
			return number;
		}

		public WeaponAttack GetAttack(WeaponBase weaponBase)
		{
			EWeapon weapon = weaponBase.weaponData.eWeapon;
			if (!weaponAttackPools.ContainsKey(weapon))
				weaponAttackPools.Add(weapon, CreateObjectPool(weaponBase.weaponData.attack, WeaponUtility.GetMaxAttacksPoolSize(weapon)));
			GameObject obj = weaponAttackPools[weapon].Get();
			if (obj == null) return null;
			if (!weaponAttacksDict.ContainsKey(obj)) weaponAttacksDict[obj] = obj.GetComponent<WeaponAttack>();
			return weaponAttacksDict[obj];
		}

		public void ReturnAttack(WeaponAttack weaponAttack)
		{
			weaponAttack.gameObject.SetActive(false);
			weaponAttackPools[weaponAttack.weaponBase.weaponData.eWeapon].Release(weaponAttack.gameObject);
		}

		public ProjectileBase GetProjectile(WeaponAttack weaponAttack)
		{
			EWeapon weapon = weaponAttack.weaponBase.weaponData.eWeapon;
			if (!projectilePools.ContainsKey(weapon))
				projectilePools.Add(weapon, CreateObjectPool(weaponAttack.prefabProjectile, WeaponUtility.GetMaxProjectilesPoolSize(weapon)));
			GameObject obj = projectilePools[weapon].Get();
			if (obj == null) return null;
			if (!projectileDict.ContainsKey(obj)) projectileDict[obj] = obj.GetComponent<ProjectileBase>();
			return projectileDict[obj];
		}

		public void ReturnProjectile(WeaponAttack weaponAttack, GameObject projectile)
		{
			projectile.SetActive(false);
			projectilePools[weaponAttack.weaponBase.weaponData.eWeapon].Release(projectile);
		}

		public AttackHit GetProjectileHit(string source, GameObject hitPrefab)
		{
			if (hitPrefab == null) hitPrefab = defaultHitPrefab;
			if (hitPrefab == null) return null;
			if (projectileHitPools == null) projectileHitPools = new Dictionary<string, ObjectPool<GameObject>>();
			if (attackHitsDict == null) attackHitsDict = new Dictionary<GameObject, AttackHit>();
			if (!projectileHitPools.TryGetValue(source, out var pool))
			{
				pool = CreateObjectPool(hitPrefab, 20);
				projectileHitPools.Add(source, pool);
			}
			GameObject obj = pool.Get();
			if (obj == null) return null;
			if (!attackHitsDict.TryGetValue(obj, out var hit)) attackHitsDict[obj] = hit = obj.GetComponent<AttackHit>();
			return hit;
		}

		public GameObject GetProjectileDoneFx(EWeapon eWeapon, GameObject hitPrefab)
		{
			if (weaponStrings == null) weaponStrings = new Dictionary<EWeapon, string>();
			if (!weaponStrings.TryGetValue(eWeapon, out var source)) weaponStrings[eWeapon] = source = eWeapon.ToString();
			return GetProjectileDoneFx(source, hitPrefab);
		}

		public GameObject GetProjectileDoneFx(string source, GameObject hitPrefab)
		{
			if (hitPrefab == null) return null;
			if (projectileDonePools == null) projectileDonePools = new Dictionary<string, ObjectPool<GameObject>>();
			if (!projectileDonePools.TryGetValue(source, out var pool))
			{
				pool = CreateObjectPoolTimeout(hitPrefab, 10, 0.5f);
				projectileDonePools.Add(source, pool);
			}
			return pool.Get();
		}

		private void InitializePoisonPool()
		{
			if (poisonCloud != null)
				poisonPool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(poisonCloud, poisonPool, 20, 1f),
					OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 5, 20);
		}

		private ObjectPool<GameObject> CreateObjectPool(GameObject hitPrefabToUse, int maxObjects)
		{
			ObjectPool<GameObject> pool = null;
			pool = new ObjectPool<GameObject>(() => CreatePooledItem(hitPrefabToUse, pool, maxObjects), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, maxObjects, maxObjects);
			return pool;
		}

		private ObjectPool<GameObject> CreateObjectPoolTimeout(GameObject hitPrefabToUse, int maxObjects, float timeoutTime)
		{
			ObjectPool<GameObject> pool = null;
			pool = new ObjectPool<GameObject>(() => CreatePooledItemTimeout(hitPrefabToUse, pool, maxObjects, timeoutTime), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, maxObjects);
			return pool;
		}

		public GameObject GetEnemyAttack(EnemySpecialAttack specialAttack)
		{
			if (enemyAttacksPools == null) enemyAttacksPools = new Dictionary<string, ObjectPool<GameObject>>();
			if (!enemyAttacksPools.TryGetValue(specialAttack.attackName, out var pool))
			{
				pool = CreateObjectPool(specialAttack.attackPrefab, 1024);
				enemyAttacksPools.Add(specialAttack.attackName, pool);
			}
			return pool.Get();
		}

		public GameObject GetEnemyAttackFx(EnemySpecialAttackPrefab enemyAttackPrefab)
		{
			if (enemyAttacksFxPools == null) enemyAttacksFxPools = new Dictionary<EEnemyAttack, ObjectPool<GameObject>>();
			if (!enemyAttacksFxPools.TryGetValue(enemyAttackPrefab.eAttack, out var pool))
			{
				if (enemyAttackPrefab.attackEffectPrefab == null) return null;
				pool = CreateObjectPool(enemyAttackPrefab.attackEffectPrefab, 256);
				enemyAttacksFxPools.Add(enemyAttackPrefab.eAttack, pool);
			}
			return pool.Get();
		}

		public void ReturnEnemyAttackFx(EnemyAttackEffectPrefab attackPrefab)
		{
			enemyAttacksFxPools[attackPrefab.eAttack].Release(attackPrefab.gameObject);
		}

		public void ReturnEnemyAttack(EnemySpecialAttackPrefab specialAttackPrefab)
		{
			enemyAttacksPools[specialAttackPrefab.specialAttack.attackName].Release(specialAttackPrefab.gameObject);
		}

		private GameObject CreatePooledItem(GameObject prefab, ObjectPool<GameObject> pool, int maxSize)
		{
			return pool.CountAll < maxSize ? Instantiate(prefab) : null;
		}

		private GameObject CreatePooledItemTimeout(GameObject prefab, ObjectPool<GameObject> pool, int maxSize, float time)
		{
			if (pool.CountAll >= maxSize) return null;
			GameObject obj = Instantiate(prefab);
			ReturnToPool returner = obj.GetComponent<ReturnToPool>();
			if (returner == null) returner = obj.AddComponent<ReturnToPool>();
			returner.SetTime(time, pool);
			return obj;
		}

		private void OnReturnedToPool(GameObject obj)
		{
			obj.SetActive(false);
		}

		private void OnTakeFromPool(GameObject obj)
		{
			if (obj != null) obj.SetActive(true);
		}

		private void OnDestroyPoolObject(GameObject obj)
		{
			Destroy(obj);
		}
	}
}
