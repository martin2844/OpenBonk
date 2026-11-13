using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class LaserBeamAttack : ConstantAttack
{
	public LineRenderer linerenderer;

	public GameObject laserStart;

	public GameObject laserEnd;

	private float radius = .5f;

	private Vector3 laserDir;

	private Enemy target;

	private float laserStopTime;

	private float laserStartedAtTime;

	private float laserReadyTime;

	private bool isShooting;

	private Vector3 prevStart;

	private Vector3 prevEnd;

	private Vector3 center;

	private Quaternion rotation;

	private Vector3 halfExtents;

	public AudioSource audioLoop;

	public GameObject explosionFx;

	private Dictionary<Collider, float> enemyHitCooldowns = new Dictionary<Collider, float>();

	private float hitCooldown = .5f;

	[Header("Whip Effect")]
	public int whipSegments = 20;

	public float whipAmplitude = 1f;

	public float whipFrequency = 2f;

	public float animateWhipTime = 1f;

	private float whipAnimationTime;

	private Vector3[] whipPoints;

	private static readonly Collider[] sphereHits = new Collider[EnemyManager.maxNumEnemiesPooled];

	private static readonly Collider[] boxHits = new Collider[EnemyManager.maxNumEnemiesPooled];

	private float laserRadius = .5f;

	private new void Awake()
	{
		base.Awake();
		WeaponInventory.A_WeaponToggled += OnWeaponToggled;
	}

	private new void OnDestroy()
	{
		base.OnDestroy();
		WeaponInventory.A_WeaponToggled -= OnWeaponToggled;
	}

	private void OnWeaponToggled(WeaponBase weaponBase)
	{
		StopLaser();
	}

	protected override void Init()
	{
		UpdateSize();
	}

	private void Update()
	{
		if (!isShooting && MyTime.time > laserReadyTime) StartLaser();
		RenderLaser();
	}

	private void StartLaser()
	{
		FindTarget();
		if (target == null) { laserReadyTime += .5f; return; }
		target.AddDebuff(EDebuff.Echo, null, GetDuration() * 2f, 1);
		isShooting = true;
		linerenderer.enabled = true;
		laserStart.SetActive(true);
		laserEnd.SetActive(true);
		linerenderer.startWidth = laserRadius;
		laserStopTime = MyTime.time + GetDuration();
		laserReadyTime = laserStopTime + GetCooldown();
		laserStartedAtTime = MyTime.time;
		prevStart = GetBeamStart();
		prevEnd = GetBeamEnd();
		whipAnimationTime = 0f;
		audioLoop.Play();
	}

	private void StopLaser()
	{
		bool wasShooting = isShooting;
		isShooting = false;
		linerenderer.enabled = false;
		laserStart.SetActive(false);
		laserEnd.SetActive(false);
		audioLoop.Stop();
		// A weapon can be toggled before its first target exists, or the target
		// can disappear while the beam is active. Neither case has a final hit.
		if (!wasShooting || target == null) return;
		explosionFx.SetActive(true);
		explosionFx.transform.position = laserEnd.transform.position;
		target.RemoveDebuff(EDebuff.Echo, false);
		var dc = WeaponUtility.GetDamageContainer(weaponBase, null, target, (GetBeamEnd() - GetBeamStart()).normalized);
		dc.damage *= GetDuration() * 3f;
		target.DamageFromPlayerWeapon(dc);
	}

	private void RenderLaser()
	{
		if (!isShooting) return;
		if (target == null) { StopLaser(); return; }
		Vector3 direction = (GetBeamEnd() - GetBeamStart()).normalized;
		Vector3 hit = target.collider.ClosestPoint(target.GetCenterPosition() - direction * 5f);
		Vector3 start = MyPlayer.Instance.playerRenderer.hips.position;
		laserStart.transform.position = start;
		Vector3 end = hit + direction * .5f;
		laserEnd.transform.position = end;
		laserStart.transform.rotation = Quaternion.LookRotation(direction);
		whipAnimationTime = Mathf.Min(animateWhipTime, whipAnimationTime + Time.deltaTime);
		float progress = whipAnimationTime / animateWhipTime;
		float eased = Easing.OutCubic(progress);
		if (whipAnimationTime < animateWhipTime)
		{
			Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
			if (perpendicular.sqrMagnitude < .01f) perpendicular = Vector3.Cross(direction, Vector3.right);
			Vector3 tip = Vector3.Lerp(start, end, eased * 2f);
			float amplitude = Mathf.Lerp(whipAmplitude, 0f, progress);
			if (whipPoints == null || whipPoints.Length != whipSegments + 1)
				whipPoints = new Vector3[whipSegments + 1];
			for (int i = 0; i <= whipSegments; i++)
			{
				float t = (float)i / whipSegments;
				float wave = Mathf.Sin(t * whipFrequency * 2f * Mathf.PI - eased * 2f * Mathf.PI);
				whipPoints[i] = Vector3.Lerp(start, tip, t) + perpendicular * (wave * amplitude * (1f + (t - 1f) * Mathf.Clamp01(eased)));
			}
			linerenderer.positionCount = whipPoints.Length;
			linerenderer.SetPositions(whipPoints);
		}
		else
		{
			linerenderer.positionCount = 2;
			linerenderer.SetPosition(0, start);
			linerenderer.SetPosition(1, end);
		}
		audioLoop.pitch = 1f + Easing.InPower(Mathf.Clamp01((MyTime.time - laserStartedAtTime) / (laserStopTime - laserStartedAtTime)), 6);
	}

	private Vector3 GetBeamStart()
	{
		return MyPlayer.Instance.playerRenderer.hips.position;
	}

	private Vector3 GetBeamEnd()
	{
		return target == null ? GetBeamStart() : target.GetCenterPosition();
	}

	private void FindTarget()
	{
		target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), 0, weaponBase.weaponData.useVision, null);
	}

	private void FixedUpdate()
	{
		if (!isShooting) return;
		if (MyTime.time > laserStopTime) { StopLaser(); return; }
		Vector3 start = GetBeamStart();
		Vector3 end = GetBeamEnd();
		Vector3 delta = end - prevStart;
		float distance = delta.magnitude;
		if (distance < Mathf.Epsilon)
		{
			int count = Physics.OverlapSphereNonAlloc(start, laserRadius, sphereHits, GameManager.Instance.whatIsEnemy);
			ProcessHits(sphereHits, count);
		}
		else
		{
			halfExtents = new Vector3(laserRadius, laserRadius, distance * .5f + laserRadius);
			center = prevStart + delta * .5f;
			rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
			int count = Physics.OverlapBoxNonAlloc(center, halfExtents, boxHits, rotation, GameManager.Instance.whatIsEnemy);
			ProcessHits(boxHits, count);
		}
		prevStart = start;
		prevEnd = end;
	}

	private void ProcessHits(Collider[] colliders, int count)
	{
		for (int i = 0; i < count; i++) HitEnemy(colliders[i]);
	}

	private void HitEnemy(Collider collider)
	{
		if (collider == target.collider) return;
		if (enemyHitCooldowns.TryGetValue(collider, out float lastHit) && hitCooldown > MyTime.time - lastHit) return;
		if (!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDead()) return;
		Vector3 direction = (prevEnd - prevStart).normalized;
		var damage = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction);
		enemy.DamageFromPlayerWeapon(damage);
		Vector3 hit = collider.ClosestPoint(enemy.GetCenterPosition() - direction * 5f);
		EffectManager.Instance.EnemyHitEffect(hit + direction * .5f, direction, enemy, weaponBase.weaponData.eWeapon, null, true);
		enemyHitCooldowns[collider] = MyTime.time;
	}

	private void UpdateSize()
	{
		laserRadius = radius * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		linerenderer.startWidth = laserRadius;
	}

	private float GetRadius()
	{
		return laserRadius;
	}

	public override float GetAuraRotationSpeed()
	{
		return 0f;
	}

	private float GetDuration()
	{
		return WeaponUtility.GetDuration(weaponBase);
	}

	private float GetCooldown()
	{
		return WeaponUtility.GetWeaponCooldown(weaponBase) + 1f;
	}

	protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon)
	{
		if (weaponBase.weaponData.eWeapon == weapon) OnStatUpdate(stat);
	}

	protected override void OnStatUpdate(EStat stat)
	{
		if (stat == EStat.SizeMultiplier) UpdateSize();
	}
}
