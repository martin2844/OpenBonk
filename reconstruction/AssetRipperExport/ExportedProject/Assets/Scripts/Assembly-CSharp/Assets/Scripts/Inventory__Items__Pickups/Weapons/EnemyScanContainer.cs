using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
	public class EnemyScanContainer
	{
		public float range;

		public float time;

		public Vector3 position;

		private const float distThreshold = 2f;

		private const float distThresholdSqr = 4f;

		private const float timeThreshold = 0.04f;

		private const float rangeThreshold = 1f;

		public EnemyScanContainer(Vector3 position, float time, float range)
		{
			Set(position, time, range);
		}

		public void Set(Vector3 position, float time, float range)
		{
			this.position = position;
			this.time = time;
			this.range = range;
		}

		public bool IsEqual(Vector3 pos, float t, float range)
		{
			return (position - pos).sqrMagnitude < distThresholdSqr && Mathf.Abs(time - t) < timeThreshold && Mathf.Abs(this.range - range) < rangeThreshold;
		}

		public bool IsEqual(EnemyScanContainer other)
		{
			return IsEqual(other.position, other.time, other.range);
		}
	}
}
