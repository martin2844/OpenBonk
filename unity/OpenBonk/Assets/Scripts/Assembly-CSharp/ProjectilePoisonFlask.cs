using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public class ProjectilePoisonFlask : ProjectileBase
{
	public Rigidbody rb;

	private float defaultProjectileRadius = 0.6f;

	private float maxProjectileSpeed = 40f;

	protected static Collider[] enemyCollidersBuffer = new Collider[EnemyManager.maxNumEnemiesPooled];

	public float explosionRadius = 5f;

	public EffectPlayer effect;

	private Vector3 explosionSizeDefault;

	protected override bool TryInit(int projectileIndex)
	{
		if (effect == null || rb == null || MyPlayer.Instance == null || weaponBase?.weaponData == null) return false;
		if (explosionSizeDefault == Vector3.zero) explosionSizeDefault = effect.transform.localScale;
		transform.localScale = Vector3.one;
		projectileRadius = defaultProjectileRadius;
		transform.position = MyPlayer.Instance.transform.position;
		rb.MovePosition(MyPlayer.Instance.transform.position);
		Enemy target = EnemyTargeting.GetEnemy(transform.position, WeaponUtility.GetWeaponRange(weaponBase), projectileIndex, weaponBase.weaponData.useVision, null);
		if (target == null || target.enemyMovement == null || target.enemyMovement.rb == null) return false;
		float speed = GetSpeed();
		if (speed <= 0f) return false;
		float travelTime = Vector3.Distance(transform.position, target.GetCenterPosition()) / speed;
		Vector3 predicted = target.GetCenterPosition() + target.enemyMovement.rb.velocity * travelTime;
		Vector3 displacement = predicted - transform.position;
		Vector3 horizontal = new Vector3(displacement.x, 0f, displacement.z);
		float distance = horizontal.magnitude;
		float gravity = Mathf.Abs(Physics.gravity.y);
		float speedSquared = speed * speed;
		float discriminant = speedSquared * speedSquared - gravity * (gravity * distance * distance + 2f * displacement.y * speedSquared);
		float angle = discriminant < 0f ? 10f * Mathf.Deg2Rad : Mathf.Min(Mathf.Atan((speedSquared + Mathf.Sqrt(discriminant)) / (gravity * distance)), Mathf.Atan((speedSquared - Mathf.Sqrt(discriminant)) / (gravity * distance)));
		rb.velocity = horizontal.normalized * (speed * Mathf.Cos(angle)) + Vector3.up * (speed * Mathf.Sin(angle));
		return true;
	}

	private float GetSpeed()
	{
		return Mathf.Min(projectileSpeed, maxProjectileSpeed);
	}

	protected override void StepMovement()
	{
		int count = Physics.OverlapSphereNonAlloc(transform.position, projectileRadius, enemyCollidersBuffer);
		bool hit = false;
		for (int i = 0; i < count; i++)
		{
			if (CheckCollision(enemyCollidersBuffer[i], Vector3.zero)) { hit = true; break; }
		}
		if (!gameObject.activeInHierarchy) return;
		Vector3 movement = GetMovementDirection().normalized;
		projectileSpeed = rb.velocity.magnitude * Time.fixedDeltaTime;
		if (!hit && Physics.SphereCastNonAlloc(transform.position, projectileRadius, movement, raycastBuffer, projectileSpeed, GameManager.Instance.whatIsProjectileRaycast, QueryTriggerInteraction.Ignore) > 0)
			CheckCollision(raycastBuffer[0].collider, raycastBuffer[0].normal);
	}

	protected override bool HitEnemy(Collider collider, Vector3 normal)
	{
		ExplodeFlask();
		return true;
	}

	protected override void HitOther(Collider collider, Vector3 normal)
	{
		ExplodeFlask();
	}

	private float GetExplosionRadius()
	{
		return explosionRadius * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
	}

	private float GetPoisonDuration()
	{
		return WeaponUtility.GetDuration(weaponBase);
	}

	private int GetNumPoisonStacks()
	{
		return Mathf.FloorToInt(weaponBase.GetValue(EStat.DamageMultiplier));
	}

	private void ExplodeFlask()
	{
		if (effect != null)
		{
			effect.transform.SetParent(null);
			effect.transform.position = transform.position;
			effect.gameObject.SetActive(false);
			effect.gameObject.SetActive(true);
			effect.transform.localScale = explosionSizeDefault * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
			effect.Play();
		}
		int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, GetExplosionRadius(), out Collider[] colliders);
		for (int i = 0; i < count; i++)
		{
			if (!EnemyManager.Instance.GetEnemy(colliders[i], out Enemy enemy)) continue;
			Vector3 direction = enemy.GetCenterPosition() - MyPlayer.Instance.transform.position;
			var damage = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction);
			enemy.DamageFromPlayerWeapon(damage);
			int stacks = GetNumPoisonStacks();
			for (int j = 0; j < stacks; j++) enemy.AddDebuff(EDebuff.Poison, damage, GetPoisonDuration(), 1);
			weaponAttack.ProjectileHit(enemy.collider.ClosestPoint(MyPlayer.Instance.transform.position), GetMovementDirection(), true, true);
		}
		ProjectileDone();
	}

	protected override void MyFixedUpdate()
	{
	}

	protected override void MyUpdate()
	{
	}

	protected override void FindMovementDirection()
	{
	}

	protected override Vector3 GetMovementDirection()
	{
		return rb != null ? rb.velocity.normalized : Vector3.zero;
	}
}
