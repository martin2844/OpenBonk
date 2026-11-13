using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;

public class ChunkersAttack : ConstantAttack
{
	public RandomSfx regenSfx;

	public RotatingProjectiles rotatingProjectiles;

	private int currentAmount;

	private float shieldReadyAtTime;

	private float rotationSpeed;

	private float cooldown;

	private float minCooldown;

	private float duration;

	private int amount;

	private float startTime;

	private float stopTime;

	private float nextStartTime;

	private bool isAttacking;

	protected override void Init()
	{
		rotatingProjectiles.SetWeapon(weaponBase);
		foreach (EStat stat in System.Enum.GetValues(typeof(EStat))) OnStatUpdate(stat);
	}

	private void FindNewTimes()
	{
		startTime = MyTime.time;
		stopTime = startTime + duration;
		nextStartTime = stopTime + cooldown;
	}

	public void StartAttack()
	{
		isAttacking = true;
		FindNewTimes();
		rotatingProjectiles.Show();
	}

	public void StopAttack()
	{
		isAttacking = false;
		rotatingProjectiles.Hide();
	}

	private void FixedUpdate()
	{
		if (!isAttacking && MyTime.time >= nextStartTime) StartAttack();
		else if (isAttacking && MyTime.time >= stopTime) StopAttack();
	}

	public override float GetAuraRotationSpeed()
	{
		return 0f;
	}

	protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon)
	{
		if (weapon == weaponBase.weaponData.eWeapon) OnStatUpdate(stat);
	}

	protected override void OnStatUpdate(EStat stat)
	{
		switch (stat)
		{
			case EStat.SizeMultiplier: SetSize(); break;
			case EStat.DurationMultiplier: SetDuration(); break;
			case EStat.ProjectileSpeedMultiplier: SetProjectileSpeed(); break;
			case EStat.AttackSpeed: SetCooldown(); break;
			case EStat.Projectiles: SetProjectiles(); break;
		}
	}

	private void SetDuration()
	{
		duration = WeaponUtility.GetDuration(weaponBase);
		rotatingProjectiles.SetDuration(duration);
	}

	private void SetProjectiles()
	{
		amount = WeaponUtility.GetAttackQuantity(weaponBase);
		rotatingProjectiles.SetAmount(amount);
	}

	private void SetSize()
	{
		rotatingProjectiles.SetSize(WeaponUtility.GetAttackSizeMultiplier(weaponBase));
	}

	private void SetProjectileSpeed()
	{
		rotatingProjectiles.SetSpeed(WeaponUtility.GetProjectileSpeed(weaponBase) * 50f);
	}

	private void SetCooldown()
	{
		minCooldown = weaponBase.weaponData.endCooldown * .1f;
		cooldown = UnityEngine.Mathf.Max(WeaponUtility.GetWeaponCooldown(weaponBase), minCooldown);
	}
}
