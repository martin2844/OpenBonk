using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Utility;
using UnityEngine;

public class FinalFightController : MonoBehaviour
{
	public Transform bossSpawnPoint;

	public Enemy boss;

	public BossPylon[] pylons;

	private List<BossPylon> activePylons = new List<BossPylon>();

	public GameObject orbFollowing;

	public GameObject orbShooty;

	public GameObject orbBleed;

	public GameObject stealWeaponWui;

	public AudioSource audioPylonsSpawn;

	public static Action<bool> A_BossDefeated;

	public static Action<float> A_BossDefeatedTime;

	public static bool isFightingFinalBoss;

	private int numWeaponsToTake;

	private int numWeaponsTaken;

	private float takeWeaponAtTime;

	private float weaponTakeInterval = 3f;

	private float healInterval = 0.5f;

	private float nextHealTime;

	private float bossDeadGracePeriod = 10f;

	private float nextOrbsFollowingTime;

	private float nextOrbsShootyTime;

	private float nextOrbsBleedTime;

	private float lastSpecialAttackTime;

	private float goonsDeadAtTime;

	private float goonSpawnInterval = 60f;

	private List<Enemy> goons = new List<Enemy>();

	public static Action A_PylonsStarted;

	public bool isBossDefeated => boss == null;

	public int currentPhase { get; private set; }

	private void Awake()
	{
		isFightingFinalBoss = true;
		BossPylon.A_Charged += OnPylonCharged;
		Enemy.A_Damage += OnEnemyDamage;
		Enemy.A_EnemyReleasedFromPool += OnEnemyReleasedFromPool;
		PlayerHealth.A_Died += OnPlayerDied;
	}

	private void OnDestroy()
	{
		isFightingFinalBoss = false;
		BossPylon.A_Charged -= OnPylonCharged;
		Enemy.A_Damage -= OnEnemyDamage;
		Enemy.A_EnemyReleasedFromPool -= OnEnemyReleasedFromPool;
		PlayerHealth.A_Died -= OnPlayerDied;
	}

	public void SpawnBoss()
	{
		Vector3 position = RaycastUtility.RayToGround(bossSpawnPoint.position + Vector3.up, 10f);
		boss = EnemyManager.Instance.SpawnBoss(MapController.currentStage.stageTimeline.boss.enemyName, 0,
			EEnemyFlag.StageBoss | EEnemyFlag.FinalBoss, position);
		boss.teleportTime = 3.5f;
		numWeaponsToTake = MyPlayer.Instance.inventory.weaponInventory.GetNumWeapons();
		takeWeaponAtTime = MyTime.time + 4f;
		nextOrbsFollowingTime = MyTime.time + 30f;
		nextOrbsShootyTime = MyTime.time + 15f;
		nextOrbsBleedTime = MyTime.time + 45f;
		goonsDeadAtTime = MyTime.time;
		isFightingFinalBoss = true;
	}

	private void FixedUpdate()
	{
		if (boss == null || boss.hp <= 0f) return;
		PylonHealing();
		SpecialAttacks();
		TakeWeapons();
	}

	private void TakeWeapons()
	{
		if (numWeaponsTaken >= numWeaponsToTake || MyTime.time < takeWeaponAtTime) return;
		takeWeaponAtTime = MyTime.time + weaponTakeInterval;
		TakeWeapon();
		numWeaponsTaken++;
	}

	private void OnEnemyReleasedFromPool(Enemy enemy)
	{
		if (goons.Remove(enemy) && goons.Count == 0) goonsDeadAtTime = MyTime.time;
		if (enemy != boss) return;
		isFightingFinalBoss = false;
		foreach (BossPylon pylon in activePylons) pylon.Despawn();
		activePylons.Clear();
		foreach (Enemy goon in goons.ToArray()) goon.Kill();
		RestoreWeapons();
		boss = null;
		float defeatedAt = MyTime.stageTimer;
		float minimumStageTime = MapController.currentStage.stageTimeline.GetStageTime() - bossDeadGracePeriod;
		if (MyTime.stageTimer < minimumStageTime) MyTime.stageTimer = minimumStageTime;
		A_BossDefeated?.Invoke(false);
		A_BossDefeatedTime?.Invoke(defeatedAt);
	}

	private void OnPlayerDied()
	{
		RestoreWeapons();
	}

	private void RestoreWeapons()
	{
		WeaponInventory inventory = MyPlayer.Instance.inventory.weaponInventory;
		foreach (WeaponBase weapon in inventory.weapons.Values)
			inventory.ToggleWeapon(weapon.weaponData.eWeapon, true);
	}

	private void StartPhase(int phase)
	{
		currentPhase = phase;
		StartPylons();
	}

	public void TakeWeapon()
	{
		WeaponInventory inventory = MyPlayer.Instance.inventory.weaponInventory;
		var weapons = new List<WeaponBase>(inventory.weapons.Values);
		for (int i = weapons.Count - 1; i > 0; i--)
		{
			WeaponBase weapon = weapons[i];
			if (!weapon.enabled) continue;
			inventory.ToggleWeapon(weapon.weaponData.eWeapon, false);
			if (EffectManager.Instance != null)
				EffectManager.Instance.TakeItem(weapon.weaponData, boss.transform,
					boss.GetCenterPosition() - boss.transform.position, 3f, 1f, 1.5f);
			return;
		}
	}

	private void GiveWeaponBack()
	{
		WeaponInventory inventory = MyPlayer.Instance.inventory.weaponInventory;
		var weapons = new List<WeaponBase>(inventory.weapons.Values);
		foreach (WeaponBase weapon in weapons)
		{
			if (weapon.enabled) continue;
			inventory.ToggleWeapon(weapon.weaponData.eWeapon, true);
			if (EffectManager.Instance != null) EffectManager.Instance.GiveItem(weapon.weaponData);
			return;
		}
	}

	private void SpecialAttacks()
	{
		float interval = 45f - currentPhase * 6f;
		if (MyTime.time > nextOrbsFollowingTime)
		{
			SpawnOrbsFollowing();
			nextOrbsFollowingTime = MyTime.time + UnityEngine.Random.Range(interval * 0.6f, interval * 1.4f);
		}
		if (MyTime.time > nextOrbsShootyTime)
		{
			SpawnOrbsShooty();
			nextOrbsShootyTime = MyTime.time + UnityEngine.Random.Range(interval * 0.6f, interval * 1.4f);
		}
		if (MyTime.time > nextOrbsBleedTime)
		{
			SpawnOrbsBleed();
			nextOrbsBleedTime = MyTime.time + UnityEngine.Random.Range(interval * 0.6f, interval * 1.4f);
		}
		if (MyTime.time > goonsDeadAtTime + goonSpawnInterval && goons.Count == 0) SpawnGoons();
	}

	private void SpawnOrbsFollowing()
	{
		var orbs = new List<GameObject>();
		int count = GetNumOrbs();
		for (int i = 0; i < count; i++)
		{
			GameObject orb = Instantiate(orbFollowing, boss.transform.position, Quaternion.identity);
			orb.GetComponent<BossOrb>().Set(i * 0.5f, currentPhase, boss, currentPhase + 1, i);
			orbs.Add(orb);
		}
		IgnoreCollisions(orbs);
	}

	private void SpawnOrbsShooty()
	{
		var orbs = new List<GameObject>();
		int count = GetNumOrbs();
		for (int i = 0; i < count; i++)
		{
			GameObject orb = Instantiate(orbShooty, boss.GetCenterPosition(), Quaternion.identity);
			orb.GetComponent<BossOrbShooty>().Set(boss, currentPhase, currentPhase + 1, i);
			orbs.Add(orb);
		}
		IgnoreCollisions(orbs);
	}

	private void SpawnOrbsBleed()
	{
		var orbs = new List<GameObject>();
		int count = GetNumOrbs();
		for (int i = 0; i < count; i++)
		{
			GameObject orb = Instantiate(orbBleed, boss.transform.position, Quaternion.identity);
			orb.GetComponent<BossOrbBleed>().Set(boss, currentPhase, currentPhase + 1, i);
			orbs.Add(orb);
		}
		IgnoreCollisions(orbs);
	}

	private int GetNumOrbs()
	{
		return Mathf.Min(currentPhase + 1, 3);
	}

	private void IgnoreCollisions(List<GameObject> orbs)
	{
		for (int i = 0; i < orbs.Count; i++)
		{
			Collider first = orbs[i].GetComponent<Collider>();
			if (first == null) continue;
			for (int j = i + 1; j < orbs.Count; j++)
			{
				Collider second = orbs[j].GetComponent<Collider>();
				if (second != null) Physics.IgnoreCollision(first, second);
			}
		}
	}

	private void SpawnGoons()
	{
		goons = new List<Enemy>();
		for (int i = 0; i < currentPhase + 2; i++)
		{
			EnemyData data = DataManager.Instance.GetEnemyData(MapController.currentMap.stages[0].stageTimeline.boss.enemyName);
			Enemy goon = EnemyManager.Instance.SpawnEnemy(data, 0, true);
			if (goon == null) continue;
			goon.DisableSpecialAttacks();
			goon.SetSpeedMultiplier(0.6f);
			goon.maxHp = boss.maxHp * 0.06f;
			goon.SetMinibossGoon(goon.maxHp);
			goons.Add(goon);
		}
	}

	private bool ArePylonsActive()
	{
		return activePylons.Count > 0 && boss != null;
	}

	private void OnPylonCharged(BossPylon pylon)
	{
		activePylons.Remove(pylon);
		if (activePylons.Count == 0) PylonsDone();
	}

	private void PylonsDone()
	{
		boss.MakeInvulnerable(false);
		GiveWeaponBack();
	}

	private void StartPylons()
	{
		boss.MakeInvulnerable(true);
		activePylons = new List<BossPylon>();
		var available = new List<int>();
		for (int i = 0; i < pylons.Length; i++) available.Add(i);
		for (int i = 0; i < 3; i++)
		{
			int choice = UnityEngine.Random.Range(0, available.Count);
			activePylons.Add(pylons[available[choice]]);
			available.RemoveAt(choice);
		}
		foreach (BossPylon pylon in activePylons) pylon.Set(boss);
		audioPylonsSpawn.Play();
		A_PylonsStarted?.Invoke();
	}

	private void PylonHealing()
	{
		if (MyTime.time < nextHealTime) return;
		nextHealTime = MyTime.time + healInterval;
		if (ArePylonsActive() && boss.hp < boss.maxHp)
			boss.Heal(Mathf.RoundToInt(boss.maxHp * 0.023f / 60f * healInterval * pylons.Length));
	}

	private void OnEnemyDamage(Enemy enemy, DamageContainer dc)
	{
		if (enemy != boss || boss == null || boss.hp <= 0f) return;
		float fraction = boss.hp / boss.maxHp;
		if (fraction <= 0.25f && currentPhase <= 2) StartPhase(3);
		else if (fraction <= 0.5f && currentPhase <= 1) StartPhase(2);
		else if (fraction <= 0.75f && currentPhase <= 0) StartPhase(1);
	}
}
