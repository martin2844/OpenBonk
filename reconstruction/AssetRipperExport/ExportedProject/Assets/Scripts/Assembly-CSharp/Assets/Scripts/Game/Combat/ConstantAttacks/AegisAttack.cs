using System;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.ConstantAttacks
{
	public class AegisAttack : ConstantAttack
	{
		public RandomSfx shieldUseSfx;

		public RandomSfx shieldRegenSfx;

		public Transform renderer;

		public AegisRenderer aegisRenderer;

		public Transform[] particles;

		public ParticleSystem rootParticles;

		private bool isActive;

		private int currentAmount;

		private float minAegisCooldown = .2f;

		private float shieldReadyAtTime;

		public static Action<int> A_Used;

		public static Action<int> A_Regen;

		private float nextAmountTime;

		protected override void Init()
		{
			RegenShield();
		}

		private void FixedUpdate()
		{
			if (!isActive && MyTime.time > shieldReadyAtTime) RegenShield();
			if (MyTime.time > nextAmountTime) AmplifyShield();
		}

		public void RegenShield()
		{
			renderer.gameObject.SetActive(true);
			shieldRegenSfx.Play();
			isActive = true;
			currentAmount = 0;
			AmplifyShield();
		}

		private void AmplifyShield()
		{
			ResetNextAmountTime();
			if (!isActive || currentAmount >= GetMaxShields()) return;
			currentAmount++;
			aegisRenderer.SetAmount(currentAmount);
			shieldRegenSfx.Play();
			A_Regen?.Invoke(currentAmount);
		}

		private void ResetNextAmountTime()
		{
			nextAmountTime = MyTime.time + WeaponUtility.GetWeaponCooldown(weaponBase) * 1.5f;
		}

		private int GetMaxShields()
		{
			return WeaponUtility.GetAttackQuantity(weaponBase);
		}

		public void UseShield(Vector3 enemyFeetPosition)
		{
			float size = WeaponUtility.GetAttackSizeMultiplier(weaponBase);
			foreach (Transform particle in particles) particle.localScale = Vector3.one * (5f * size);
			rootParticles.gameObject.SetActive(true);
			Vector3 direction = enemyFeetPosition + Vector3.up * transform.localPosition.y - transform.position;
			rootParticles.transform.rotation = Quaternion.LookRotation(direction);
			rootParticles.Play();
			bool useSound = true;
			foreach (Collider collider in RaycastUtility.ConeCastAll(transform.position, direction, 10f * size, 60f))
			{
				if (!EnemyManager.Instance.GetEnemy(collider, out var enemy)) continue;
				enemy.DamageFromPlayerWeapon(WeaponUtility.GetDamageContainer(weaponBase, null, enemy, direction));
				EffectManager.Instance.EnemyHitEffect(collider.ClosestPoint(transform.position), direction, enemy, weaponBase.weaponData.eWeapon, null, useSound);
				useSound = false;
			}
			currentAmount--;
			aegisRenderer.SetAmount(currentAmount);
			if (currentAmount <= 0) { renderer.gameObject.SetActive(false); isActive = false; }
			shieldUseSfx.Play();
			shieldReadyAtTime = MyTime.time + Mathf.Max(WeaponUtility.GetWeaponCooldown(weaponBase), minAegisCooldown);
			ResetNextAmountTime();
			A_Used?.Invoke(currentAmount);
		}

		public bool IsActive()
		{
			return isActive;
		}

		protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon)
		{
		}

		protected override void OnStatUpdate(EStat stat)
		{
			if (stat != EStat.AttackSpeed) return;
			float ready = MyTime.time + WeaponUtility.GetWeaponCooldown(weaponBase) * 1.5f;
			shieldReadyAtTime = Mathf.Min(shieldReadyAtTime, ready);
			nextAmountTime = Mathf.Min(nextAmountTime, ready);
		}

		public override float GetAuraRotationSpeed()
		{
			return -3.5f;
		}
	}
}
