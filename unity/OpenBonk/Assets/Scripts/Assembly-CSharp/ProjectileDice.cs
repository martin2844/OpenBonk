using System;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Assets.Scripts.Game.Combat;
using UnityEngine;

public class ProjectileDice : ProjectileBasic
{
	public float explosionRadius = 5f;

	public GameObject diceFx;

	public GameObject diceFx6;

	public RotateObjectRandomly rotator;

	private float fxCooldown = 0.06f;

	private float maxScale = 2.5f;

	private int diceRoll;

	public static Action A_RollSix;

	private string explosionFxName = "diceFx";

	private string explosionFxName6 = "diceFx6";

	protected override bool TryInit(int projectileIndex)
	{
		currentTarget = null;
		var target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null) return false;
		direction = (target.GetCenterPosition() - transform.position).normalized;
		if (rotator != null) rotator.FindNewRotation();
		diceRoll = global::Utility.MyRandom.random.Next(1, 7);
		if (transform.localScale.x > maxScale) transform.localScale = Vector3.one * maxScale;
		return true;
	}

	protected override bool CheckCollision(Collider collider, Vector3 normal)
	{
		if (collider == null || GameManager.Instance == null || (GameManager.Instance.whatIsProjectileRaycast.value & (1 << collider.gameObject.layer)) == 0) return false;
		Hitscan(collider);
		ProjectileDone();
		return true;
	}

	private void Hitscan(Collider collider)
	{
		float damage = diceRoll + weaponBase.GetValue(EStat.DamageMultiplier);
		float radius = explosionRadius * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, radius, out Collider[] colliders);
		bool firstHit = true;
		for (int i = 0; i < count; i++)
		{
			if (!EnemyManager.Instance.GetEnemy(colliders[i], out var enemy)) continue;
			Vector3 hitDirection = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
			var damageContainer = WeaponUtility.GetDamageContainer(weaponBase, null, enemy, hitDirection, damage);
			damageContainer.element = EElement.Neutral;
			enemy.DamageFromPlayerWeapon(damageContainer);
			weaponAttack.ProjectileHit(colliders[i].ClosestPoint(transform.position), damageContainer.direction, true, firstHit);
			firstHit = false;
		}
		EWeapon weapon = weaponBase.weaponData.eWeapon;
		if (!FxUtility.weaponCooldowns.ContainsKey(weapon)) FxUtility.weaponCooldowns.Add(weapon, 0f);
		if (MyTime.time > FxUtility.weaponCooldowns[weapon])
		{
			FxUtility.weaponCooldowns[weapon] = MyTime.time + fxCooldown;
			if (PoolManager.Instance != null)
			{
				GameObject effect = PoolManager.Instance.GetProjectileDoneFx(diceRoll == 6 ? explosionFxName6 : explosionFxName, diceRoll == 6 ? diceFx6 : diceFx);
				if (effect != null) effect.transform.position = transform.position;
			}
		}
		if (diceRoll == 6) A_RollSix?.Invoke();
	}
}
