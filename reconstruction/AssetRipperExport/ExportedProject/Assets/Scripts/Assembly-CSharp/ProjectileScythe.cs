using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class ProjectileScythe : ProjectileBase
{
	public class MeleeHit
	{
		public Vector3 pos;
		public Vector3 dir;
		public MeleeHit(Vector3 pos, Vector3 dir) { this.pos = pos; this.dir = dir; }
	}
	public Vector3 colliderOffset = new Vector3(0f, 0f, .5f);
	public float testMultiplier = 1f;
	private float upOffset;
	private float bigHitMultiplierSize = 1.5f;
	private float bigHitMultiplierDamage = 2f;
	private bool isThisHitBig;
	public ParticleSystem ps;
	private ParticleSystem.MainModule main;
	public Color defaultColor;
	public Color bigHitColor;
	public static bool nextHitIsBig;
	private static float nextBigHitReadyTime;
	private float bigHitCooldown = 2f;
	private List<MeleeHit> effectHits = new List<MeleeHit>();
	private bool useAudio = true;

	protected override bool TryInit(int projectileIndex)
	{
		if (weaponBase == null || weaponBase.weaponData == null || weaponAttack == null || MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null) return false;
		isThisHitBig = nextHitIsBig;
		if (isThisHitBig)
		{
			nextHitIsBig = false;
			projectileRadius *= bigHitMultiplierSize;
			transform.localScale *= bigHitMultiplierSize;
		}
		if (ps != null)
		{
			main = ps.main;
			Color color = isThisHitBig ? bigHitColor : defaultColor;
			color.a = main.startColor.color.a;
			if (isThisHitBig) bigHitColor.a = color.a; else defaultColor.a = color.a;
			main.startColor = color;
		}
		Transform renderer = MyPlayer.Instance.playerRenderer.transform;
		transform.SetPositionAndRotation(MyPlayer.Instance.transform.position + renderer.forward + Vector3.up * weaponBase.weaponData.spawnOffset.y, renderer.rotation);
		CheckZone(weaponBase, projectileRadius, weaponAttack.prefabHit);
		useAudio = true;
		return true;
	}

	public void CheckZone(WeaponBase weaponBase, float radius, GameObject hitEffect = null)
	{
		if (weaponBase == null || EnemyManager.Instance == null || MyPlayer.Instance == null) return;
		Vector3 offset = transform.right * colliderOffset.x + transform.up * colliderOffset.y + transform.forward * colliderOffset.z;
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position + offset * WeaponUtility.GetAttackSizeMultiplier(weaponBase), radius, out Collider[] colliders);
		effectHits = new List<MeleeHit>();
		bool scheduleEffect = hitEffect != null;
		for (int i = 0; i < count; i++)
		{
			Collider collider = colliders[i];
			if (!EnemyManager.Instance.GetEnemy(collider, out var enemy) || enemy == null) continue;
			Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			var dc = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction);
			if (isThisHitBig) dc.damage *= bigHitMultiplierDamage;
			enemy.DamageFromPlayerWeapon(dc);
			effectHits.Add(new MeleeHit(collider.ClosestPoint(transform.position), dc.direction));
			if (scheduleEffect) { Invoke(nameof(SpawnEffect), .1f); scheduleEffect = false; }
			if (!nextHitIsBig && enemy.IsDead() && enemy.IsElite() && MyTime.time >= nextBigHitReadyTime)
			{
				nextHitIsBig = true;
				nextBigHitReadyTime = MyTime.time + bigHitCooldown;
			}
		}
	}

	private void SpawnEffect()
	{
		foreach (var hit in effectHits)
		{
			weaponAttack.ProjectileHit(hit.pos, hit.dir, true, useAudio);
			useAudio = false;
		}
	}
	protected override Vector3 GetMovementDirection() { return transform.forward; }
	protected override void MyFixedUpdate() { }
	protected override void MyUpdate()
	{
		if (MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null || weaponBase == null) return;
		Transform renderer = MyPlayer.Instance.playerRenderer.transform;
		transform.SetPositionAndRotation(MyPlayer.Instance.transform.position + renderer.forward + Vector3.up * weaponBase.weaponData.spawnOffset.y, renderer.rotation);
	}
	protected override void FindMovementDirection() { }
	private float GetRadius() { return projectileRadius; }
	protected override bool CheckCollision(Collider collider, Vector3 normal) { return false; }
	protected override void StepMovement() { }
	protected override void CheckSpawnCollision() { }
}
