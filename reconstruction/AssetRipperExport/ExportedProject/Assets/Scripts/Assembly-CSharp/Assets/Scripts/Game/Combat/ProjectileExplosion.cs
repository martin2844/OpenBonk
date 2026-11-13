using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using UnityEngine;

namespace Assets.Scripts.Game.Combat
{
	public class ProjectileExplosion : MonoBehaviour
	{
		private WeaponAttack weaponAttack;

		public GameObject collisionEffect;

		private Vector3 effectPos;

		private Vector3 effectDir;

		private bool useAudio;

		public void Set(WeaponAttack weaponAttack, float radius, Vector3 position, float defaultRadius)
		{
		}

		public void CheckZone(WeaponBase weaponBase, float radius, GameObject hitEffect = null)
		{
		}

		private void SpawnEffect()
		{
		}
	}
}
