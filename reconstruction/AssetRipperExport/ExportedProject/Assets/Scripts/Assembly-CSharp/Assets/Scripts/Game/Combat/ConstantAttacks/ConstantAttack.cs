using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.ConstantAttacks
{
	public abstract class ConstantAttack : MonoBehaviour
	{
		public WeaponBase weaponBase;

		public void Set(WeaponBase weaponBase)
		{
			this.weaponBase = weaponBase;
			Init();
		}

		protected void Awake()
		{
			PlayerStatsNew.A_StatUpdate += OnStatUpdate;
			WeaponBase.A_WeaponStatUpdate += OnWeaponStatUpdate;
		}

		protected void OnDestroy()
		{
			PlayerStatsNew.A_StatUpdate -= OnStatUpdate;
			WeaponBase.A_WeaponStatUpdate -= OnWeaponStatUpdate;
		}

		protected abstract void Init();

		protected abstract void OnWeaponStatUpdate(EStat stat, EWeapon weapon);

		protected abstract void OnStatUpdate(EStat stat);

		public abstract float GetAuraRotationSpeed();

		public virtual bool IsManualRotation()
		{
			return false;
		}
	}
}
