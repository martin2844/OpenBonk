namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles
{
	public class ProjectileUtility
	{
		public static float GetArrowSpeed(WeaponBase weaponBase)
		{
			return WeaponUtility.GetProjectileSpeed(weaponBase) + WeaponUtility.GetAttackSizeMultiplier(weaponBase) * 0.25f;
		}

		public static float GetArrowSpeedReduction(WeaponBase weaponBase)
		{
			return GetArrowSpeed(weaponBase) / (WeaponUtility.GetDuration(weaponBase) / 0.02f);
		}
	}
}
