using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
	public static class EnemyTargeting
	{
		private static int currentBufferCount;

		private static Collider[] enemyBuffer = new Collider[EnemyManager.maxNumEnemiesPooled + 1];

		private static RaycastHit[] losBuf = new RaycastHit[1];

		private static EnemyScanContainer currentBufferContainer = new EnemyScanContainer(Vector3.one * -9999f, -9999f, -1f);

		private static readonly Dictionary<Type, Collider[]> buffers = new Dictionary<Type, Collider[]>();

		private static readonly object nullType = typeof(object);

		private static readonly RaycastHit[] raycastBuffer = new RaycastHit[1];

		public static void Init()
		{
			GameManager.A_RunStarted += Reset;
		}

		public static void Cleanup()
		{
			GameManager.A_RunStarted -= Reset;
		}

		private static void Reset()
		{
			buffers.Clear();
		}

		public static int GetEnemiesInRadiusSafe(object owner, Vector3 pos, float range, out Collider[] buffer)
		{
			int count = GetEnemiesInRadius(pos, range, out Collider[] enemies);
			Type ownerType = (owner ?? nullType).GetType();
			if (!buffers.TryGetValue(ownerType, out buffer))
			{
				buffer = new Collider[EnemyManager.maxNumEnemiesPooled + 1];
				buffers[ownerType] = buffer;
			}
			for (int i = 0; i < count; i++) buffer[i] = enemies[i];
			return count;
		}

		public static Enemy GetEnemy(Vector3 position, float range, int projectileIndex, bool useVision, GameObject exceptObject)
		{
			float ratio = SaveManager.Instance.config.cfGameSettings.random_enemy_targeting;
			int count = GetEnemiesInRadius(position, range, out Collider[] candidates);
			return ShouldPickRandom(projectileIndex, ratio)
				? GetRandomEnemy(candidates, count, position, useVision, exceptObject)
				: GetTargetedEnemy(candidates, count, position, useVision, exceptObject);
		}

		public static Enemy GetRandomEnemyInRadius(Vector3 position, float range, bool useVision, GameObject exceptObject)
		{
			int count = GetEnemiesInRadius(position, range, out Collider[] candidates);
			return GetRandomEnemy(candidates, count, position, useVision, exceptObject);
		}

		private static int GetEnemiesInRadius(Vector3 position, float range, out Collider[] enemies)
		{
			if (!currentBufferContainer.IsEqual(position, MyTime.time, range))
			{
				currentBufferCount = Physics.OverlapSphereNonAlloc(position, range, enemyBuffer, GameManager.Instance.whatIsEnemy, QueryTriggerInteraction.Ignore);
				currentBufferContainer.Set(position, MyTime.time, range);
			}
			enemies = enemyBuffer;
			return currentBufferCount;
		}

		private static Enemy GetTargetedEnemy(Collider[] colliders, int count, Vector3 pos, bool useVision, GameObject exceptObject)
		{
			return SaveManager.Instance.config.cfGameSettings.enemy_targeting_mode == 0
				? GetSmartEnemy(colliders, count, pos, useVision, exceptObject)
				: GetClosestEnemy(colliders, count, pos, useVision, exceptObject);
		}

		private static Enemy GetSmartEnemy(Collider[] colliders, int count, Vector3 pos, bool useVision, GameObject exceptObject)
		{
			if (count == 0) return null;
			Vector3 facing = MyPlayer.Instance.playerRenderer.transform.forward.normalized;
			float bestScore = float.NegativeInfinity;
			Enemy best = null;
			for (int i = 0; i < count; i++)
			{
				Collider collider = colliders[i];
				if (!collider || collider.gameObject == exceptObject || !EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDeadOrDyingNextFrame()) continue;
				Vector3 offset = enemy.GetCenterPosition() - pos;
				if (offset.sqrMagnitude < 0.001f) continue;
				float distance = offset.magnitude;
				float facingScore = Mathf.Max(0f, Vector3.Dot(offset.normalized, facing));
				float score = 1f / (distance + 1f) + facingScore * 0.2f;
				if (distance < 6f && facingScore < 0.2f) score += 2f;
				if (enemy.IsBoss()) score += 0.5f;
				if (score <= bestScore) continue;
				if (useVision && Physics.RaycastNonAlloc(pos, offset.normalized, raycastBuffer, distance, GameManager.Instance.whatIsProjectileObstruction, QueryTriggerInteraction.Ignore) > 0) continue;
				best = enemy;
				bestScore = score;
			}
			return best;
		}

		private static Enemy GetClosestEnemy(Collider[] colliders, int count, Vector3 pos, bool useVision, GameObject exceptObject)
		{
			float bestDistance = float.MaxValue;
			Enemy best = null;
			for (int i = 0; i < count; i++)
			{
				Collider collider = colliders[i];
				if (!collider || collider.gameObject == exceptObject || !EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDeadOrDyingNextFrame()) continue;
				Vector3 offset = enemy.GetCenterPosition() - pos;
				float distanceSqr = offset.sqrMagnitude;
				if (distanceSqr >= bestDistance) continue;
				if (useVision)
				{
					float distance = Mathf.Sqrt(distanceSqr);
					if (distance < 0.001f || Physics.Raycast(pos, offset.normalized, distance, GameManager.Instance.whatIsProjectileObstruction, QueryTriggerInteraction.Ignore)) continue;
				}
				best = enemy;
				bestDistance = distanceSqr;
			}
			return best;
		}

		private static Enemy GetRandomEnemy(Collider[] colliders, int count, Vector3 pos, bool useVision, GameObject exceptObject)
		{
			if (count == 0) return null;
			int start = UnityEngine.Random.Range(0, count);
			for (int i = 0; i < count; i++)
			{
				Collider collider = colliders[(start + i) % count];
				if (!collider || collider.gameObject == exceptObject || !EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy.IsDeadOrDyingNextFrame()) continue;
				if (useVision)
				{
					Vector3 offset = enemy.GetCenterPosition() - pos;
					float distance = offset.magnitude;
					if (distance < 0.001f || Physics.Raycast(pos, offset.normalized, distance, GameManager.Instance.whatIsProjectileObstruction, QueryTriggerInteraction.Ignore)) continue;
				}
				return enemy;
			}
			return null;
		}

		private static bool ShouldPickRandom(int idx, float ratio)
		{
			if (idx <= 0) return false;
			ratio = Mathf.Clamp01(ratio);
			return ratio >= 1f || (ratio > 0f && Mathf.Floor((idx + 1) * ratio) > Mathf.Floor(idx * ratio));
		}
	}
}
