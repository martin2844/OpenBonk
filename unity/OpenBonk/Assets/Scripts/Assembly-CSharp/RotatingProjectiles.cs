using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using Utility;

public class RotatingProjectiles : MonoBehaviour
{
	public GameObject prefab;

	public float baseProjectileRadius = 1f;

	public AudioSource audioSpawn;

	public AudioSource audioLoop;

	private float baseLoopVolume;

	private List<ParticleSystem> prefabs;

	private int amount;

	private float projectileRadius;

	private float rotationSpeed = 50f;

	private const int maxQuantity = 50;

	private float maxRotationSpeed = 450f;

	private Vector3[] rockPositions = new Vector3[50];

	private List<RaycastUtility.ConeSphere> debugSpheres = new List<RaycastUtility.ConeSphere>();

	private Dictionary<int, Dictionary<Collider, float>> projectileEnemiesCooldowns = new Dictionary<int, Dictionary<Collider, float>>();

	private float enemyHitCooldown = .4f;

	public WeaponBase weaponBase;

	private float fadeTimer;

	private float fadeTime = .25f;

	private bool isActive;

	private float startTime;

	private float endTime;

	private float duration;

	public float baseDistance = 4f;

	private Vector3 defaultScale;

	private Vector3 projectileScale;

	private float scaleMultiplier = 1f;

	private float distance;

	public void SetWeapon(WeaponBase weaponBase)
	{
		this.weaponBase = weaponBase;
		TryInit();
	}

	private void TryInit()
	{
		if (prefabs != null) return;
		prefabs = new List<ParticleSystem> { prefab.GetComponent<ParticleSystem>() };
		defaultScale = prefab.transform.localScale;
		projectileRadius = baseProjectileRadius;
		baseLoopVolume = audioLoop.volume;
	}

	private void Update()
	{
		TryInit();
		if (fadeTimer < fadeTime) fadeTimer = Mathf.Clamp01(fadeTimer + MyTime.deltaTime);
		float fade = Mathf.Clamp01(fadeTimer / fadeTime);
		float size = isActive ? fade : 1f - fade;
		audioLoop.volume = baseLoopVolume * size;
		float expansion = Mathf.Clamp01((MyTime.time - startTime) / (endTime - startTime)) * distance;
		for (int i = 0; i < amount; i++)
		{
			Vector3 direction = Quaternion.Euler(0f, i * 360f / amount, 0f) * Vector3.forward;
			prefabs[i].transform.localPosition = direction * (MyPlayer.Instance.width + 2f + expansion);
			prefabs[i].transform.localScale = projectileScale * size;
		}
		transform.Rotate(Vector3.up, rotationSpeed * MyTime.deltaTime, Space.Self);
	}

	private bool CanHitbox()
	{
		return isActive || fadeTimer < fadeTime;
	}

	private void FixedUpdate()
	{
		StepHitboxes();
	}

	private void StepHitboxes()
	{
		if (!CanHitbox()) return;
		debugSpheres.Clear();
		for (int i = 0; i < prefabs.Count; i++)
		{
			if (!prefabs[i].gameObject.activeInHierarchy) continue;
			Vector3 position = prefabs[i].transform.position;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, position, projectileRadius, out Collider[] hits);
			for (int j = 0; j < count; j++) HitEnemy(i, hits[j]);
			rockPositions[i] = prefabs[i].transform.position;
		}
	}

	private bool HitEnemy(int projectileIndex, Collider collider)
	{
		if (collider == null) return false;
		if (!projectileEnemiesCooldowns.TryGetValue(projectileIndex, out var hits))
			projectileEnemiesCooldowns[projectileIndex] = hits = new Dictionary<Collider, float>();
		if (hits.TryGetValue(collider, out float lastHit) && enemyHitCooldown > MyTime.time - lastHit) return false;
		if (!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDead()) return false;
		Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
		enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction));
		EffectManager.Instance.EnemyHitEffect(collider.ClosestPoint(transform.position), direction, enemy, weaponBase.weaponData.eWeapon, null, true);
		hits[collider] = MyTime.time;
		return true;
	}

	private void OnDrawGizmosSelected()
	{
	}

	public void Show()
	{
		fadeTimer = 0f;
		isActive = true;
		audioSpawn.Play();
		audioLoop.Play();
		startTime = MyTime.time;
		endTime = startTime + duration;
		gameObject.SetActive(false);
		gameObject.SetActive(true);
	}

	public void Hide()
	{
		fadeTimer = 0f;
		isActive = false;
	}

	public void SetAmount(int newAmount)
	{
		TryInit();
		amount = Mathf.Clamp(newAmount, 0, maxQuantity - 1);
		for (int i = 0; i < amount; i++)
		{
			if (i >= prefabs.Count) prefabs.Add(Instantiate(prefab, transform).GetComponent<ParticleSystem>());
			prefabs[i].gameObject.SetActive(true);
		}
		for (int i = amount; i < prefabs.Count; i++) prefabs[i].gameObject.SetActive(false);
	}

	public void SetSize(float multiplier)
	{
		TryInit();
		scaleMultiplier = multiplier;
		projectileScale = defaultScale * multiplier;
		projectileRadius = baseProjectileRadius * multiplier;
		distance = baseDistance + projectileRadius + multiplier * .33f;
	}

	public void SetSpeed(float speed)
	{
		rotationSpeed = Mathf.Clamp(speed, 0f, maxRotationSpeed);
	}

	public void SetDuration(float duration)
	{
		this.duration = duration;
	}
}
