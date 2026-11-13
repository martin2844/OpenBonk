using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using UnityEngine;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Pickups;

public class PickupManager : MonoBehaviour
{
	public LayerMask whatIsPickups;

	public GameObject hastePrefab;

	public GameObject healthPrefab;

	public GameObject magnetPrefab;

	public GameObject nukePrefab;

	public GameObject ragePrefab;

	public GameObject shieldPrefab;

	public GameObject stonksPrefab;

	public GameObject timeFreezePrefab;

	private PickupStackableList xpList;

	private PickupStackableList goldList;

	private float stackRadius = 0.5f;

	private float stackRadiusMin = 0.5f;

	private float stackRadiusMax = 8f;

	private float stackRadiusMaxTime = 900f;

	public static int maxXpObjects = 300;

	public static int maxGoldObjects = 300;

	public static PickupManager Instance;

	private Vector3 hoverHeight = Vector3.up * 2f;

	private EPickup[] powerups = { EPickup.Haste, EPickup.Health, EPickup.Magnet, EPickup.Nuke, EPickup.Rage, EPickup.Shield, EPickup.Stonks, EPickup.Time };

	private float lastPowerupAtTime;

	public static int maxPowerupsOnMap = 50;

	private int numPowerupsOnMap;

	private static readonly Collider[] overlapResults = new Collider[301];

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		xpList = new PickupStackableList(maxXpObjects, EPickup.Xp);
		goldList = new PickupStackableList(maxGoldObjects, EPickup.Gold);
		InvokeRepeating(nameof(SlowUpdate), 0f, 10f);
		Enemy.A_EnemyDied += OnEnemyDied;
	}

	private void OnDestroy()
	{
		Enemy.A_EnemyDied -= OnEnemyDied;
		if (Instance == this) Instance = null;
	}

	private void SlowUpdate()
	{
		stackRadius = Mathf.Lerp(stackRadiusMin, stackRadiusMax, GameManager.Instance.GetAliveTime() / stackRadiusMaxTime);
	}

	private void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
	{
		Vector3 position = enemy.transform.position;
		var inventory = MyPlayer.Instance != null ? MyPlayer.Instance.inventory : null;
		int extraShards = inventory?.itemInventory?.GetItem(EItem.EchoShard) is ItemEchoShard echo ? echo.GetExtraShards() : 0;
		if (!ChallengesTracker.HasChallengeModifier("no_xp"))
			for (int i = 0; i <= extraShards; i++)
			{
				Pickup pickup = SpawnPickup(EPickup.Xp, position, i == 0 ? enemy.GetXp() : 1);
				if (i > 0 && pickup != null)
				{
					XpVisuals visuals = pickup.GetComponent<XpVisuals>();
					if (visuals != null) visuals.SetEchoXp();
				}
			}
		if (CanSpawnPowerup() && global::Utility.MyRandom.random.NextDouble() < GetPowerupDropChance(enemy))
		{
			EffectManager.Instance.SpawnPickupOrb(GetRandomPowerup(), enemy.GetCenterPosition());
			lastPowerupAtTime = MyTime.time;
		}
		if (inventory?.statusEffects != null && inventory.statusEffects.HasStatusEffect(EStatusEffect.Stonks))
			EffectManager.Instance.GoldBurstEffect(position);
	}

	private EPickup GetRandomPowerup()
	{
		return powerups[global::Utility.MyRandom.random.Next(0, powerups.Length)];
	}

	private float GetPowerupDropChance(Enemy enemy)
	{
		return enemy.GetPowerupDropChance() * PlayerStats.GetStat(EStat.PowerupChance) * GetPowerupTimeMultiplier();
	}

	private float GetPowerupTimeMultiplier()
	{
		return Mathf.Clamp01((MyTime.time - lastPowerupAtTime) / 120f) * 2f;
	}

	public Pickup SpawnPickup(EPickup ePickup, Vector3 pos, int value, bool useRandomOffsetPosition = true, float pickupDelay = 0f)
	{
		pos = global::Utility.RaycastUtility.RayToGround(pos, 9999f);
		pos += hoverHeight;
		if (useRandomOffsetPosition)
		{
			Vector3 offset = Random.insideUnitSphere.normalized;
			pos += new Vector3(offset.x, 0f, offset.z);
		}
		if (ePickup != EPickup.Xp && ePickup != EPickup.Gold)
		{
			Pickup powerup = SpawnPooledPickup(ePickup);
			if (powerup != null) powerup.Set(pos, value, pickupDelay);
			return powerup;
		}
		if (CheckOverlap(ePickup, pos, out Pickup existing)) { existing.AddValue(value); return ePickup == EPickup.Xp ? existing : null; }
		Pickup pickup = (ePickup == EPickup.Xp ? xpList : goldList).AddPickup(pos);
		if (pickup != null) pickup.Set(pos, value, pickupDelay);
		return pickup;
	}

	public void CountAdd()
	{
		numPowerupsOnMap++;
	}

	public void PowerupCountRemove()
	{
		numPowerupsOnMap--;
	}

	public bool CanSpawnPowerup()
	{
		return numPowerupsOnMap < maxPowerupsOnMap;
	}

	private Pickup SpawnPooledPickup(EPickup ePickup)
	{
		var pool = PoolManager.Instance.powerupPool;
		if (pool == null || (pool.CountInactive == 0 && pool.CountAll >= maxPowerupsOnMap)) return null;
		GameObject obj = pool.Get();
		if (obj == null) return null;
		var generic = obj.GetComponent<GenericPowerupPrefab>();
		if (generic == null || generic.pickup == null) { pool.Release(obj); return null; }
		generic.Set(ePickup);
		return generic.pickup;
	}

	public void PickupTriggered(Pickup pickup)
	{
		if (pickup.ePickup == EPickup.Xp) xpList.RemovePickup(pickup);
		else if (pickup.ePickup == EPickup.Gold) goldList.RemovePickup(pickup);
		DespawnPickup(pickup);
	}

	public void PickupAllXp()
	{
		Transform player = GameManager.Instance.GetPlayer().transform;
		foreach (Pickup pickup in xpList.pickupsList) pickup.StartFollowingPlayer(player);
	}

	private bool CheckOverlap(EPickup ePickup, Vector3 pos, out Pickup overlappingPickup)
	{
		overlappingPickup = null;
		int count = Physics.OverlapSphereNonAlloc(pos, stackRadius, overlapResults, whatIsPickups);
		for (int i = 0; i < count; i++)
		{
			Pickup candidate = overlapResults[i].GetComponent<Pickup>();
			if (candidate != null && candidate.ePickup == ePickup) { overlappingPickup = candidate; return true; }
		}
		return false;
	}

	public GameObject GetNewPickup(EPickup pickup, Vector3 pos)
	{
		GameObject obj = (pickup == EPickup.Xp ? PoolManager.Instance.xpPool : PoolManager.Instance.goldPool).Get();
		obj.transform.position = pos;
		return obj;
	}

	public void DespawnPickup(Pickup pickup)
	{
		if (pickup.ePickup == EPickup.Xp) PoolManager.Instance.xpPool.Release(pickup.gameObject);
		else if (pickup.ePickup == EPickup.Gold) PoolManager.Instance.goldPool.Release(pickup.gameObject);
		else { PowerupCountRemove(); PoolManager.Instance.powerupPool.Release(pickup.gameObject); }
	}
}
