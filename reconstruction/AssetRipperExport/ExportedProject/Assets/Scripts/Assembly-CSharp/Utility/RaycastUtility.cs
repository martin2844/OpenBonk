using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

namespace Utility
{
	public class RaycastUtility
	{
		public struct ConeSphere
		{
			public Vector3 pos;

			public float radius;

			public ConeSphere(Vector3 position, float radius)
			{
				pos = position;
				this.radius = radius;
			}
		}

		private static Collider[] coneCastBuffer = new Collider[EnemyManager.maxNumEnemiesPooled + 1];

		public static Vector3 RayToGround(Vector3 pos, float maxDistance = 9999f)
		{
			return RayToGround(pos, GameManager.Instance.whatIsGround, maxDistance);
		}

		public static Vector3 RayToGround(Vector3 pos, LayerMask layerMask, float maxDistance = 9999f)
		{
			if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, maxDistance, layerMask) && hit.collider.gameObject.name != "WorldCollider") return hit.point;
			return pos;
		}

		public static List<Collider> ConeCastAll(Vector3 origin, Vector3 direction, float maxDistance, float coneAngle)
		{
			Vector3 forward = new Vector3(direction.x, 0f, direction.z).normalized;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(null, origin, maxDistance, out Collider[] candidates);
			var result = new List<Collider>();
			for (int i = 0; i < count; i++)
			{
				Vector3 offset = candidates[i].transform.position - origin;
				offset.y = 0f;
				if (Vector3.Angle(forward, offset.normalized) < coneAngle * .5f) result.Add(candidates[i]);
			}
			return result;
		}

		public static HashSet<Collider> ConeCastNew(Vector3 origin, Vector3 direction, float distance, float coneAngle)
		{
			var result = new HashSet<Collider>();
			if (GameManager.Instance == null) return result;
			foreach (ConeSphere sphere in GetConecastPositions(origin, direction, distance, coneAngle))
			{
				int count = Physics.OverlapSphereNonAlloc(sphere.pos, sphere.radius, coneCastBuffer, GameManager.Instance.whatIsEnemy);
				for (int i = 0; i < count; i++) if (coneCastBuffer[i] != null) result.Add(coneCastBuffer[i]);
			}
			return result;
		}

		public static List<ConeSphere> GetConecastPositions(Vector3 pos, Vector3 dir, float dist, float coneAngle)
		{
			var result = new List<ConeSphere>();
			if (dist <= 0f) return result;
			float endRadius = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * dist;
			float slope = (endRadius - 1f) / dist;
			result.Add(new ConeSphere(pos, 1f));
			float denominator = 1f - slope;
			if (denominator > 0f)
			{
				float step = 2f / denominator;
				while (step < dist && result.Count < 1024)
				{
					float radius = 1f + slope * step;
					result.Add(new ConeSphere(pos + dir * step, radius));
					float next = step + 2f * radius / denominator;
					if (next <= step) break;
					step = next;
				}
			}
			result.Add(new ConeSphere(pos + dir * dist, endRadius));
			return result;
		}
	}
}
