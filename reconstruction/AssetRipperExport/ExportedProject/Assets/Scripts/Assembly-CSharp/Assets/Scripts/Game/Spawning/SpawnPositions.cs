using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Spawning
{
	public static class SpawnPositions
	{
		public static Vector3 INVALID_POS = new Vector3(69.69f, 69.69f, 69.69f);

		private static RaycastHit[] spherecastBuffer;

		private static float nextFindBoundsTime;

		private static float minX;

		private static float minZ;

		private static float maxX;

		private static float maxZ;

		private static List<RaycastHit> myRaycastAllBuffer;

		private static RaycastHit[] myRaycastAllArrayBuffer;

		private static string layerNoSpawnArea;

		private static string layerCameraIgnore;

		private static string layerWater;

		private static string layerObject;

		private static string layerGround;

		private static string tagIgnore = "Ignore";

		public static Vector3 GetEnemySpawnPosition(EnemyData enemyData, int attempts = 50, bool useDirectionBias = true, float maxDistance = 3.4028235E+38f)
		{
			float bias = MyPlayer.Instance.GetSpawnDirectionBias();
			return GetEnemySpawnPositionBiased(enemyData, useDirectionBias ? bias : 0f, attempts, maxDistance);
		}

		public static Vector3 GetPositionAroundPlayer(float maxDistance)
		{
			return GetEnemySpawnPositionBiased(null, 0f, 50, maxDistance);
		}

		public static Vector3 GetEnemySpawnPositionTest(EnemyData enemyData, int attempts = 50)
		{
			return default(Vector3);
		}

		private static void GetSpawnDistances(out float min, out float max)
		{
			float multiplier = Mathf.Clamp(1f + MyTime.runTimer / 60f * 0.04f, 1f, 2f);
			min = 20f * multiplier;
			max = 40f * multiplier;
		}

		private static void FindBounds()
		{
			if (GameManager.Instance.isCrypt || (RsgController.Instance != null && RsgController.Instance.roomBoss.gameObject.activeInHierarchy))
			{
				minX = minZ = -9999f;
				maxX = maxZ = 9999f;
				return;
			}
			Vector3 player = MyPlayer.Instance.transform.position;
			maxX = Mathf.Max(MapInfo.mapBoundsUpper.x - 2f, player.x - 25f);
			minX = Mathf.Min(MapInfo.mapBoundsLower.x + 2f, player.x + 25f);
			maxZ = Mathf.Max(MapInfo.mapBoundsUpper.z - 2f, player.z - 25f);
			minZ = Mathf.Min(MapInfo.mapBoundsLower.z + 2f, player.z + 25f);
		}

		internal static Vector3 GetEnemySpawnPositionBiased(EnemyData enemyData, float playerDirectionBias, int attempts = 50, float maxDistance = 3.4028235E+38f)
		{
			if (MyTime.time > nextFindBoundsTime)
			{
				nextFindBoundsTime = MyTime.time + 0.1f;
				FindBounds();
			}
			GetSpawnDistances(out float minDistance, out float maxSpawnDistance);
			if (maxDistance != float.MaxValue)
			{
				minDistance = Mathf.Min(minDistance, maxDistance);
				maxSpawnDistance = Mathf.Min(maxSpawnDistance, maxDistance);
			}
			Rigidbody playerRb = GameManager.Instance.GetPlayerMovement().GetRb();
			Vector3 predicted = playerRb.position + playerRb.velocity * Assets.Scripts.Actors.Enemies.Enemy.defaultTeleportTime;
			Vector3 biasDirection = GetPlayerMovementDirection().normalized;
			biasDirection.y = 0f;
			if (biasDirection == Vector3.zero)
			{
				biasDirection = Random.onUnitSphere;
				biasDirection.y = 0f;
				biasDirection.Normalize();
			}
			playerDirectionBias = Mathf.Clamp(playerDirectionBias, 0f, 0.96f);
			for (int i = 0; i < attempts; i++)
			{
				Vector3 direction = SampleBiasedDirection(biasDirection, playerDirectionBias);
				direction.y = 0f;
				direction.Normalize();
				Vector3 candidate = predicted + direction * Random.Range(minDistance, maxSpawnDistance) + Vector3.up * 999f;
				candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
				candidate.z = Mathf.Clamp(candidate.z, minZ, maxZ);
				RaycastHit[] hits = MyRaycastAll(candidate, Vector3.down, GameManager.Instance.whatIsGroundAndObjects, 9999f, 10);
				RaycastHit hit = FindHitClosestToPlayerY(hits, out bool found, false);
				if (found) return hit.point;
			}
			return INVALID_POS;
		}

		private static RaycastHit[] MyRaycastAll(Vector3 origin, Vector3 dir, int layerMask, float maxDistance = 9999f, int maxHits = 10)
		{
			if (myRaycastAllBuffer == null) myRaycastAllBuffer = new List<RaycastHit>();
			myRaycastAllBuffer.Clear();
			float traveled = 0f;
			while (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance - traveled, layerMask))
			{
				myRaycastAllBuffer.Add(hit);
				if (myRaycastAllBuffer.Count >= maxHits) break;
				origin = hit.point + dir * 0.01f;
				traveled += hit.distance;
				if (traveled >= maxDistance) break;
			}
			myRaycastAllArrayBuffer = myRaycastAllBuffer.ToArray();
			return myRaycastAllArrayBuffer;
		}

		public static Vector3 GetEnemySpawnPositionAroundPoint(Vector3 pos, float minRadius, float maxRadius, int attempts = 50, bool onlyGround = false, float fromHeight = 999f)
		{
			for (int i = 0; i < attempts; i++)
			{
				float radius = Random.Range(minRadius, maxRadius);
				Vector3 direction = Random.insideUnitSphere;
				direction.y = 0f;
				Vector3 origin = pos + direction.normalized * radius + Vector3.up * 999f;
				LayerMask mask = onlyGround ? GameManager.Instance.whatIsGround : GameManager.Instance.whatIsGroundAndObjects;
				if (Physics.SphereCast(origin, 0.5f, Vector3.down, out RaycastHit hit, 9999f, mask) && !hit.collider.CompareTag(tagIgnore))
					return hit.point;
			}
			return INVALID_POS;
		}

		private static Vector3 SampleBiasedDirection(Vector3 biasedTowards, float bias)
		{
			if (bias <= 0f) return Random.onUnitSphere;
			if (bias >= 1f) return biasedTowards;
			float cosTheta = 1f - 2f * Mathf.Clamp01(Mathf.Pow(Random.value, bias * 10f));
			float theta = Mathf.Acos(cosTheta);
			float phi = Random.Range(0f, Mathf.PI * 2f);
			Vector3 sample = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), cosTheta);
			return (Quaternion.FromToRotation(Vector3.forward, biasedTowards) * sample).normalized;
		}

		private static Vector3 GetPlayerMovementDirection()
		{
			return MyPlayer.Instance.GetAverageMovingDirection();
		}

		public static Vector3 GetPositionAroundPoint(Vector3 center, float minRadius, float maxRadius, float spherecastRadius, int attempts = 50)
		{
			if (GameManager.Instance == null) return center;
			for (int i = 0; i < attempts; i++)
			{
				Vector3 direction = Random.insideUnitSphere;
				direction.y = 0f;
				direction.Normalize();
				Vector3 origin = center + direction * Random.Range(minRadius, maxRadius) + Vector3.up * 999f;
				if (TryGetPosition(origin, spherecastRadius, GameManager.Instance.whatIsGroundAndObjects,
					out Vector3 hitPoint, out _, true, false, 90f)) return hitPoint;
			}
			return center;
		}

		public static Vector3 GetObjectSpawnPosition(Vector3 center, Vector3 size, float checkRadius, int layerMask, out Vector3 normal, int attempts = 50, bool onlyUseGroundLayer = true, bool debug = false, bool canSpawnInWater = false, float maxSlopeAngle = 44f, float extraRayFromHeight = 0f)
		{
			for (int i = 0; i < attempts; i++)
			{
				Vector3 origin = new Vector3(center.x + Random.Range(-1f, 1f) * size.x * 0.5f,
					center.y + size.y + 999f + extraRayFromHeight,
					center.z + Random.Range(-1f, 1f) * size.z * 0.5f);
				float distance = 9999f + extraRayFromHeight;
				if (!canSpawnInWater && Physics.Raycast(origin, Vector3.down, out RaycastHit waterHit, distance) &&
					waterHit.collider.gameObject.layer == LayerMask.NameToLayer("Water")) continue;
				if (!Physics.SphereCast(new Ray(origin, Vector3.down), checkRadius, out RaycastHit hit, distance, layerMask)) continue;
				if (hit.collider == null || hit.collider.CompareTag("Ignore") || hit.collider.CompareTag("NoSpawnArea")) continue;
				if (onlyUseGroundLayer && hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground")) continue;
				if (Vector3.Angle(Vector3.up, hit.normal) > maxSlopeAngle) continue;
				normal = hit.normal;
				return hit.point;
			}
			normal = Vector3.zero;
			return INVALID_POS;
		}

		private static bool TryGetPosition(Vector3 pos, float checkRadius, int layerMask, out Vector3 hitPoint, out Vector3 normal, bool onlyUseGroundLayer = true, bool canSpawnInWater = false, float maxSlopeAngle = 90f)
		{
			hitPoint = Vector3.zero;
			normal = Vector3.zero;
			if (!canSpawnInWater && Physics.Raycast(pos, Vector3.down, out RaycastHit waterHit, 9999f) &&
				waterHit.collider.gameObject.layer == LayerMask.NameToLayer("Water")) return false;
			if (!Physics.SphereCast(new Ray(pos, Vector3.down), checkRadius, out RaycastHit hit, 9999f, layerMask)) return false;
			if (hit.collider == null || hit.collider.CompareTag("Ignore") || hit.collider.CompareTag("NoSpawnArea")) return false;
			if (onlyUseGroundLayer && hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground")) return false;
			if (Vector3.Angle(Vector3.up, hit.normal) > maxSlopeAngle) return false;
			hitPoint = hit.point;
			normal = hit.normal;
			return true;
		}

		public static RaycastHit FindHitClosestToPlayerY(RaycastHit[] hits, out bool foundPosition, bool canChooseObject = true)
		{
			foundPosition = false;
			if (MapController.currentMap.mapType == EMapType.ProceduralMesh || MapController.IsFinalBossStage())
			{
				RaycastHit best = default;
				float highest = float.NegativeInfinity;
				foreach (RaycastHit hit in hits)
				{
					if (hit.collider.CompareTag("Ignore") || hit.collider.CompareTag("CameraIgnore")) continue;
					if (hit.point.y > highest) { highest = hit.point.y; best = hit; }
					foundPosition = true;
				}
				return best;
			}
			float playerY = MyPlayer.Instance.transform.position.y;
			float closest = float.PositiveInfinity;
			// The tiled-map path retains the first hit as its fallback even if
			// every hit is filtered out; callers must still inspect foundPosition.
			RaycastHit selected = hits.Length > 0 ? hits[0] : default;
			foreach (RaycastHit hit in hits)
			{
				if (hit.collider.CompareTag("Ignore") || hit.collider.CompareTag("CameraIgnore")) continue;
				if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Object"))
				{
					foundPosition = canChooseObject;
					return canChooseObject ? hit : default;
				}
				float distance = Mathf.Abs(hit.point.y - playerY);
				if (distance < closest) { closest = distance; selected = hit; foundPosition = true; }
			}
			return selected;
		}

		public static Vector3 PredictPlayerPosition(float time)
		{
			Rigidbody playerBody = MyPlayer.Instance.playerMovement.GetComponent<Rigidbody>();
			return playerBody.position + playerBody.velocity * time;
		}

		public static Vector3 GetRandomSpawnPositionOnMap(float extraHeight = 0f)
		{
			return GetObjectSpawnPosition(Assets.Scripts.MapGeneration.MapInfo.mapCenter, Assets.Scripts.MapGeneration.MapInfo.mapSize, 0.5f, GameManager.Instance.whatIsGroundAndObjects, out _, 999, true, false, false, 44f, extraHeight);
		}
	}
}
