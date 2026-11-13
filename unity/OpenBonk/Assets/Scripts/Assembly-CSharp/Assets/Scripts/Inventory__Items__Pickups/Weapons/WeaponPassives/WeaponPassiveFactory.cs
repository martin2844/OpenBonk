namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponPassives
{
	public class WeaponPassiveFactory
	{
		public static WeaponPassive GetWeaponPassive(WeaponBase weaponBase)
		{
			if (weaponBase == null || weaponBase.weaponData == null) return null;
			switch (weaponBase.weaponData.eWeapon)
			{
				case EWeapon.BloodMagic: return new WeaponPassiveBloodMagic(weaponBase);
				case EWeapon.Dice: return new WeaponPassiveDice(weaponBase);
				default: return null;
			}
		}
	}
}
