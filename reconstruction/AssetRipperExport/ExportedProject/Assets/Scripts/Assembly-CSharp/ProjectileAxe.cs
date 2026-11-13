using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileAxe : ProjectileBase
{
	public float collisionCooldown = 0.25f;

	public TrailRenderer trailRenderer;

	private float forwardOffset;

	private float upOffset;

	public RandomSfx sfx;

	public GameObject sfxLoop;

	private Vector3 pushDir;

	private static readonly RaycastHit[] _raycastHitBuffer = new RaycastHit[1];

	private Vector3 desiredPosition;

	private Vector3 startPosition;

	private float moveTime = 0.3f;

	private float nextCheckDamageTime;

	private static Dictionary<Collider, int> numTimesEnemiesHitThisTick = new Dictionary<Collider, int>();

	private static bool hasDamage;

	private static float damageThisTick;

	private float moveTimer;

	protected override bool TryInit(int projectileIndex)
	{
		if (trailRenderer != null)
		{
			trailRenderer.Clear();
			trailRenderer.startWidth = 0.25f * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		}
		if (projectileIndex == 0)
		{
			if (sfx != null) sfx.Play();
			if (sfxLoop != null) sfxLoop.SetActive(true);
		}
		if (MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null) return false;
		Vector3 spread = Quaternion.Euler(0f, GetAngle(projectileIndex, WeaponUtility.GetAttackQuantity(weaponBase)), 0f) * Vector3.forward;
		transform.SetParent(MyPlayer.Instance.playerRenderer.transform);
		transform.localRotation = Quaternion.LookRotation(spread);
		transform.SetParent(null);
		forwardOffset = 3.4f + projectileRadius * 1.1f;
		transform.position = MyPlayer.Instance.transform.position;
		desiredPosition = transform.position + transform.forward * forwardOffset;
		moveTimer = 0f;
		startPosition = transform.position;
		nextCheckDamageTime = 0f;
		if (GameManager.Instance != null && Physics.RaycastNonAlloc(transform.position, transform.forward, _raycastHitBuffer, forwardOffset, GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore) > 0)
			desiredPosition = _raycastHitBuffer[0].point - transform.forward * projectileRadius;
		numTimesEnemiesHitThisTick.Clear();
		return true;
	}

	private float GetAngle(int projectileIndex, int maxIndex)
	{
		if (maxIndex < 2) return 0f;
		float spread = Mathf.Clamp(maxIndex * 12, 0, 348);
		return Mathf.Lerp(-spread, spread, Mathf.Clamp01((float)projectileIndex / (maxIndex - 1)));
	}

	protected override Vector3 GetMovementDirection()
	{
		return transform.forward;
	}

	protected override void MyFixedUpdate()
	{
		CheckDamage();
	}

	private void LateUpdate()
	{
		TryPopDamage();
	}

	private void CheckDamage()
	{
		if (nextCheckDamageTime > MyTime.time) return;
		nextCheckDamageTime = MyTime.time + collisionCooldown;
		Vector3 position = moveTimer < 1f ? Vector3.Lerp(startPosition, desiredPosition, UnityEngine.UIElements.Experimental.Easing.InOutCirc(moveTimer)) : transform.position;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, position, projectileRadius, out Collider[] enemies);
		for (int i = 0; i < count; i++)
		{
			Collider collider = enemies[i];
			numTimesEnemiesHitThisTick.TryGetValue(collider, out int hits);
			numTimesEnemiesHitThisTick[collider] = hits + 1;
			hasDamage = true;
		}
	}

	private void TryPopDamage()
	{
		if (!hasDamage) return;
		hasDamage = false;
		int effectIndex = 0;
		foreach (var entry in numTimesEnemiesHitThisTick)
		{
			if (!EnemyManager.Instance.GetEnemy(entry.Key, out var enemy)) continue;
			Vector3 direction = enemy.transform.position - MyPlayer.Instance.transform.position;
			direction.y = 0f;
			var damage = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction);
			damage.damage *= entry.Value;
			enemy.DamageFromPlayerWeapon(damage);
			damageThisTick += damage.damage;
			if (effectIndex < 5 && EffectManager.Instance != null)
				EffectManager.Instance.EnemyHitEffect(entry.Key.ClosestPoint(transform.position), direction, enemy != null, weaponBase.weaponData.eWeapon, null, effectIndex == 0);
			effectIndex++;
		}
		numTimesEnemiesHitThisTick.Clear();
	}

	protected override void MyUpdate()
	{
		if (moveTimer >= 1f) return;
		moveTimer = Mathf.Min(1f, moveTimer + MyTime.deltaTime / moveTime);
		transform.position = Vector3.Lerp(startPosition, desiredPosition, UnityEngine.UIElements.Experimental.Easing.InOutCirc(moveTimer));
	}

	protected override void FindMovementDirection()
	{
	}

	private float GetRadius()
	{
		return projectileRadius;
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		return false;
	}

	protected override void StepMovement()
	{
	}

	protected override void CheckSpawnCollision()
	{
	}

	private void OnDrawGizmosSelected()
	{
	}
}
