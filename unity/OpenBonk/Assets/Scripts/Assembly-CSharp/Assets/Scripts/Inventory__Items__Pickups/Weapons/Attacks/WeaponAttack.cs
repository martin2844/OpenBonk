using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Objects.Pooling;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks
{
	public class WeaponAttack : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CStartAttack_003Ed__22 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public WeaponAttack _003C_003E4__this;

			private float _003Ctimer_003E5__2;

			private int _003Cquantity_003E5__3;

			private float _003CburstInterval_003E5__4;

			private int _003Ci_003E5__5;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CStartAttack_003Ed__22(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		public GameObject prefabProjectile;

		public GameObject prefabMuzzle;

		public GameObject prefabHit;

		public WeaponBase weaponBase;

		protected MyPlayer player;

		private bool attackDone;

		public float projectileSizeMultiplier = 1f;

		public Action A_SpawnedProjectile;

		public static Action<ProjectileBase> A_SpawnedProjectileSuccessfully;

		private float expirationTime;

		private bool isAttacking;

		private int maxNumProjectilesWithoutInterval = 80;

		private float timer;

		private int attackQuantity;

		private int attackQuantityCurrent;

		private float burstInterval;

		private static readonly WaitForEndOfFrame waitEndOfFrame;

		private EnemyScanContainer lastCheckSphere = new EnemyScanContainer(Vector3.zero, -1f, -1f);

		private EnemyScanContainer reuseContainer = new EnemyScanContainer(Vector3.zero, -1f, -1f);

		public bool lastWasSkip;

		private float muzzleCooldown = 0.1f;

		private AttackMuzzle muzzle;

		public float damageMultiplier = 1f;

		public void SetAttack(WeaponBase weaponBase, MyPlayer player, float damageMultiplier = 1f)
		{
			attackDone = false;
			this.damageMultiplier = damageMultiplier;
			this.weaponBase = weaponBase;
			this.player = player;
			StartAttackNoCoroutine();
		}

		private void StartAttackNoCoroutine()
		{
			isAttacking = true;
			attackQuantityCurrent = 0;
			attackQuantity = WeaponUtility.GetAttackQuantity(weaponBase);
			burstInterval = WeaponUtility.GetBurstInterval(weaponBase);
			timer = burstInterval;
			if (muzzle != null) muzzle.Set(attackQuantity, burstInterval);
		}

		private void FixedUpdate()
		{
			FixedUpdateAttack();
		}

		private void FixedUpdateAttack()
		{
			if (!isAttacking || attackQuantityCurrent >= attackQuantity) return;
			timer += MyTime.fixedDeltaTime;
			if (burstInterval <= 0f)
			{
				while (attackQuantityCurrent < attackQuantity && attackQuantityCurrent < maxNumProjectilesWithoutInterval)
				{
					SpawnProjectile(attackQuantityCurrent);
					attackQuantityCurrent++;
				}
			}
			else if (timer >= burstInterval)
			{
				timer = 0f;
				SpawnProjectile(attackQuantityCurrent);
				attackQuantityCurrent++;
			}
			if (attackQuantityCurrent >= attackQuantity || (burstInterval <= 0f && attackQuantityCurrent >= maxNumProjectilesWithoutInterval)) StopAttackNoCoroutine();
		}

		private void StopAttackNoCoroutine()
		{
			isAttacking = false;
			attackDone = true;
			expirationTime = MyTime.time + WeaponUtility.GetDuration(weaponBase) + 0.5f;
		}

		[IteratorStateMachine(typeof(_003CStartAttack_003Ed__22))]
		private IEnumerator StartAttack()
		{
			return null;
		}

		protected virtual void SpawnProjectile(int projectileIndex)
		{
			float range = WeaponUtility.GetWeaponRange(weaponBase);
			if (weaponBase.weaponData.onlySpawnWhenCloseEnemies)
			{
				reuseContainer.Set(transform.position, MyTime.time, range);
				if (reuseContainer.IsEqual(lastCheckSphere))
				{
					if (lastWasSkip) return;
				}
				else
				{
					lastCheckSphere.Set(reuseContainer.position, reuseContainer.time, reuseContainer.range);
					lastWasSkip = EnemyTargeting.GetEnemiesInRadiusSafe(this, transform.position, range, out _) == 0;
					if (lastWasSkip) return;
				}
			}
			ProjectileBase projectile = PoolManager.Instance.GetProjectile(this);
			if (projectile == null) return;
			projectile.gameObject.SetActive(true);
			projectile.transform.localScale = prefabProjectile.transform.localScale * GetSize();
			projectile.transform.rotation = GetProjectileRotation();
			projectile.transform.position = GetProjectilePosition();
			projectile.Set(weaponBase, this, projectileIndex);
			A_SpawnedProjectile?.Invoke();
		}

		public float GetSize()
		{
			return WeaponUtility.GetAttackSizeMultiplier(weaponBase) * projectileSizeMultiplier - projectileSizeMultiplier + 1f;
		}

		public void ProjectileDone(ProjectileBase projectile)
		{
			projectile.gameObject.SetActive(false);
			PoolManager.Instance.ReturnProjectile(this, projectile.gameObject);
		}

		public void ProjectileHit(Vector3 hitPos, Vector3 moveDir, bool hitEnemy, bool useSfx)
		{
			if (prefabHit == null) return;
			EffectManager.Instance.EnemyHitEffect(hitPos, moveDir, hitEnemy, weaponBase.weaponData.eWeapon, prefabHit, useSfx);
		}

		public void SuccessfullySpawnedProjectile(ProjectileBase projectile)
		{
			if (prefabMuzzle != null)
			{
				EWeapon weapon = weaponBase.weaponData.eWeapon;
				if (!FxUtility.muzzleCooldowns.ContainsKey(weapon)) FxUtility.muzzleCooldowns.Add(weapon, 0f);
				if (MyTime.time >= FxUtility.muzzleCooldowns[weapon])
				{
					FxUtility.muzzleCooldowns[weapon] = MyTime.time + muzzleCooldown;
					if (muzzle != null)
					{
						if (!muzzle.gameObject.activeSelf) muzzle.gameObject.SetActive(true);
						muzzle.transform.SetPositionAndRotation(projectile.transform.position, projectile.transform.rotation);
					}
					else
					{
						muzzle = Instantiate(prefabMuzzle, projectile.transform.position, projectile.transform.rotation).GetComponent<AttackMuzzle>();
						muzzle.Set(WeaponUtility.GetAttackQuantity(weaponBase), WeaponUtility.GetBurstInterval(weaponBase));
					}
					muzzle.Play();
				}
			}
			A_SpawnedProjectileSuccessfully?.Invoke(projectile);
		}

		private void Update()
		{
			if (attackDone && MyTime.time >= expirationTime) AttackTimeout();
		}

		private void AttackTimeout()
		{
			if (muzzle != null) muzzle.gameObject.SetActive(false);
			gameObject.SetActive(false);
			PoolManager.Instance.ReturnAttack(this);
		}

		private Vector3 GetProjectilePosition()
		{
			return player.transform.position + Vector3.up * weaponBase.weaponData.spawnOffset.y;
		}

		private Quaternion GetProjectileRotation()
		{
			return player.playerMovement.playerCam.rotation;
		}
	}
}
