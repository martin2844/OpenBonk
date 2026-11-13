using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Managers;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles
{
	public class ProjectileDexecutioner : ProjectileBase
	{
		private struct MeleeHit
		{
			public Vector3 pos;
			public Vector3 dir;
			public MeleeHit(Vector3 pos, Vector3 dir) { this.pos = pos; this.dir = dir; }
		}

		public Vector3 colliderOffset = new Vector3(0f, 0f, .5f);
		public float testMultiplier = 1f;
		public float projectileDistance = 5f;
		private float forwardOffset;
		private float upOffset;
		public Vector3 attackDir = Vector3.forward;
		public float executionChance = .1f;
		private static readonly RaycastHit[] sphereHits = new RaycastHit[EnemyManager.maxNumEnemiesPooled];
		private DamageContainer dcExecute = new DamageContainer(0f, "");
		private List<MeleeHit> effectHits = new List<MeleeHit>();
		private bool useAudio = true;

		protected override bool TryInit(int projectileIndex)
		{
			if (weaponBase == null || weaponBase.weaponData == null || weaponAttack == null || MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null) return false;
			forwardOffset = weaponBase.weaponData.spawnOffset.z * WeaponUtility.GetAttackSizeMultiplier(weaponBase);
			var target = EnemyTargeting.GetEnemy(transform.position, forwardOffset + projectileDistance, projectileIndex, weaponBase.weaponData.useVision, null);
			attackDir = MyPlayer.Instance.playerRenderer.transform.forward;
			if (target != null) attackDir = (target.GetFeetPosition() + Vector3.up * (MyPlayer.Instance.height * .5f) - MyPlayer.Instance.transform.position).normalized;
			transform.position = MyPlayer.Instance.transform.position + attackDir * forwardOffset + Vector3.up * weaponBase.weaponData.spawnOffset.y;
			if (attackDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(attackDir);
			CheckZone(weaponBase, projectileRadius, weaponAttack.prefabHit);
			useAudio = true;
			return true;
		}

		public void CheckZone(WeaponBase weaponBase, float radius, GameObject hitEffect = null)
		{
			if (weaponBase == null || GameManager.Instance == null || EnemyManager.Instance == null || MyPlayer.Instance == null) return;
			float size = WeaponUtility.GetAttackSizeMultiplier(weaponBase);
			Vector3 forward = transform.forward;
			Vector3 origin = transform.position + forward * (colliderOffset.z * size - radius * 2f);
			effectHits.Clear();
			bool scheduleEffect = hitEffect != null;
			int count = Physics.SphereCastNonAlloc(origin, radius, forward, sphereHits, projectileDistance * size, GameManager.Instance.whatIsEnemy, QueryTriggerInteraction.Collide);
			for (int i = 0; i < count; i++)
			{
				Collider hit = sphereHits[i].collider;
				if (!EnemyManager.Instance.GetEnemy(hit, out var enemy) || enemy == null) continue;
				Vector3 effectDirection = forward;
				if (global::Utility.MyRandom.random.NextDouble() < executionChance)
				{
					dcExecute.Reuse(0f, weaponBase.weaponData.damageSourceName);
					dcExecute.enemy = enemy;
					DamageUtility.ApplyExecute(dcExecute);
					enemy.DamageFromPlayerWeapon(dcExecute);
				}
				else
				{
					Vector3 direction = (enemy.GetCenterPosition() - MyPlayer.Instance.transform.position).normalized;
					var dc = WeaponUtility.GetDamageContainer(weaponBase, this, enemy, direction);
					enemy.DamageFromPlayerWeapon(dc);
					effectDirection = dc.direction;
				}
				if (effectHits.Count >= 10) continue;
				effectHits.Add(new MeleeHit(hit.ClosestPoint(transform.position), effectDirection));
				if (scheduleEffect) { Invoke(nameof(SpawnEffect), .1f); scheduleEffect = false; }
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

		protected override Vector3 GetMovementDirection() { return attackDir; }
		protected override void MyFixedUpdate() { }
		protected override void MyUpdate()
		{
			if (MyPlayer.Instance != null && weaponBase != null && weaponBase.weaponData != null)
				transform.position = MyPlayer.Instance.transform.position + attackDir * forwardOffset + Vector3.up * weaponBase.weaponData.spawnOffset.y;
		}
		protected override void FindMovementDirection() { }
		private float GetRadius() { return projectileRadius; }
		protected override bool CheckCollision(Collider collider, Vector3 normal) { return false; }
		protected override void StepMovement() { }
		protected override void CheckSpawnCollision() { }
	}
}
