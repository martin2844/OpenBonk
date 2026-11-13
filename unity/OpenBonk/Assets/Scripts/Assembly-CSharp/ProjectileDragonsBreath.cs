using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class ProjectileDragonsBreath : ConstantAttack
{
	public ParticleSystem ps;
	public AudioSource sfx;
	public AudioClip sfxStart;
	public AudioClip sfxLoop;
	private float defaultVolume;
	private ParticleSystem.VelocityOverLifetimeModule[] velocities;
	private ParticleSystem[] particles;
	private float startTime;
	private float stopTime;
	private float previousStopTime;
	private float stopHitboxTime;
	private Dictionary<Collider, float> enemyHitCooldowns = new Dictionary<Collider, float>();
	private float enemyHitCooldown = .25f;
	private float hitboxCooldown = .06f;
	private float nextHitboxTime;
	private bool isActive;
	private float range;
	private float duration;
	private float rotationSpeed;
	private float cooldown;
	private float minCooldown = 1f;
	private Vector3 attackDir;
	private float nextFindTargetTime;
	private float findTargetInterval = .25f;
	private Enemy enemyTarget;
	private float lingerTime = .2f;
	private float scaleTimer;
	private float scaleOverTime = .8f;
	private float oldRange;
	private float scale = 2f;
	private float oldScale = 2f;

	protected override void Init()
	{
		particles = ps != null ? ps.GetComponentsInChildren<ParticleSystem>() : new ParticleSystem[0];
		velocities = new ParticleSystem.VelocityOverLifetimeModule[particles.Length];
		for (int i = 0; i < particles.Length; i++) velocities[i] = particles[i].velocityOverLifetime;
		OnStatUpdate(EStat.SizeMultiplier);
		OnStatUpdate(EStat.AttackSpeed);
		OnStatUpdate(EStat.DurationMultiplier);
		OnStatUpdate(EStat.ProjectileSpeedMultiplier);
		startTime = MyTime.time + cooldown;
		stopTime = startTime + duration;
		if (sfx != null) defaultVolume = sfx.volume;
	}

	private void Update()
	{
		if (MyTime.paused) return;
		if (MyPlayer.Instance != null) transform.position = MyPlayer.Instance.transform.position;
		SizeUpdate();
		UpdateSfx();
	}

	private void FindClosestTarget()
	{
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, range + 5f, out Collider[] hits);
		float closest = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			var hit = hits[i];
			if (hit == null) continue;
			float distance = Vector3.Distance(transform.position, hit.transform.position);
			if (distance >= closest || EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(hit, out var enemy)) continue;
			enemyTarget = enemy;
			closest = distance;
		}
	}

	private void FixedUpdate()
	{
		if (weaponBase == null || MyPlayer.Instance == null) return;
		if (MyTime.time >= nextFindTargetTime) FindClosestTarget();
		StepActive();
		if (isActive)
		{
			Quaternion desired = MyPlayer.Instance.playerRenderer != null ? MyPlayer.Instance.playerRenderer.transform.rotation : transform.rotation;
			if (enemyTarget == null) FindClosestTarget();
			else
			{
				Vector3 targetDirection = enemyTarget.GetFeetPosition() - MyPlayer.Instance.GetFeetPosition();
				if (targetDirection != Vector3.zero) desired = Quaternion.LookRotation(targetDirection);
				if (enemyTarget.IsDeadOrDyingNextFrame()) enemyTarget = null;
			}
			transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, rotationSpeed * Time.fixedDeltaTime);
			attackDir = transform.forward;
		}
		if (!isActive && MyTime.time >= stopHitboxTime) return;
		if (ps != null && MyPlayer.Instance.playerMovement != null)
		{
			Vector3 wish = MyPlayer.Instance.playerMovement.GetWishDir();
			Vector3 projection = Vector3.Project(MyPlayer.Instance.playerMovement.GetVelocity(), wish);
#pragma warning disable 618
			ps.startSpeed = projection.magnitude + 15f;
#pragma warning restore 618
		}
		if (MyTime.time < nextHitboxTime) return;
		nextHitboxTime = MyTime.time + hitboxCooldown;
		foreach (Collider collider in global::Utility.RaycastUtility.ConeCastNew(transform.position, attackDir, range, 30f)) HitEnemy(collider);
	}

	private bool HitEnemy(Collider collider)
	{
		if (collider == null || EnemyManager.Instance == null || MyPlayer.Instance == null) return false;
		if (enemyHitCooldowns.TryGetValue(collider, out float lastTime) && MyTime.time - lastTime < enemyHitCooldown) return false;
		if (!EnemyManager.Instance.GetEnemy(collider, out var enemy) || enemy == null || enemy.IsDead()) return false;
		Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
		enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction));
		if (EffectManager.Instance != null)
			EffectManager.Instance.EnemyHitEffect(collider.ClosestPoint(transform.position), direction, true, weaponBase.weaponData.eWeapon, null, true);
		enemyHitCooldowns[collider] = MyTime.time;
		return true;
	}

	private void StepActive()
	{
		if (isActive && MyTime.time >= stopTime)
		{
			isActive = false;
			foreach (var particle in particles) if (particle != null) particle.Stop();
			previousStopTime = stopTime;
			startTime = MyTime.time + cooldown;
			stopTime = startTime + duration;
			stopHitboxTime = MyTime.time + lingerTime;
		}
		if (!isActive && MyTime.time > startTime)
		{
			isActive = true;
			foreach (var particle in particles) if (particle != null) particle.Play();
			if (sfx != null) { sfx.clip = sfxStart; sfx.loop = false; sfx.volume = defaultVolume; sfx.Play(); }
			if (MyPlayer.Instance != null && MyPlayer.Instance.playerRenderer != null) transform.rotation = MyPlayer.Instance.playerRenderer.transform.rotation;
		}
	}

	private bool IsAttacking() { return isActive; }
	private void SizeUpdate()
	{
		if (scaleTimer >= 1f) return;
		scaleTimer = Mathf.Min(1f, scaleTimer + MyTime.deltaTime / scaleOverTime);
		transform.localScale = Vector3.one * Mathf.Lerp(oldScale, scale, Easing.InOutCirc(scaleTimer));
	}
	protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon)
	{
		if (weaponBase != null && weaponBase.weaponData.eWeapon == weapon) OnStatUpdate(stat);
	}
	protected override void OnStatUpdate(EStat stat)
	{
		if (weaponBase == null) return;
		switch (stat)
		{
			case EStat.SizeMultiplier: UpdateSize(); break;
			case EStat.ProjectileSpeedMultiplier: rotationSpeed = PlayerStats.GetStat(EStat.ProjectileSpeedMultiplier) * 1500f; break;
			case EStat.DurationMultiplier: duration = WeaponUtility.GetDuration(weaponBase); break;
			case EStat.AttackSpeed: UpdateCooldown(); break;
		}
	}
	private void UpdateSize()
	{
		scaleTimer = 0f;
		range = 8f * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		oldScale = scale;
		scale = 2f * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
	}
	private void UpdateCooldown()
	{
		minCooldown = weaponBase.weaponData.endCooldown * .5f;
		cooldown = Mathf.Max(WeaponUtility.GetWeaponCooldown(weaponBase), minCooldown);
	}
	public override bool IsManualRotation() { return true; }
	public override float GetAuraRotationSpeed() { return 0f; }
	private void UpdateSfx()
	{
		if (sfx == null) return;
		if (isActive && !sfx.isPlaying) { sfx.clip = sfxLoop; sfx.loop = true; sfx.Play(); }
		else if (!isActive && sfx.isPlaying)
		{
			sfx.volume = Mathf.Lerp(defaultVolume, 0f, (MyTime.time - previousStopTime) / .6f);
			if (sfx.volume <= 0f) { sfx.Stop(); sfx.volume = 1f; }
		}
	}
	private void OnDrawGizmosSelected() { }
}
