using UnityEngine;

namespace Assets.Scripts.MapGeneration
{
	public static class MapInfo
	{
		public static Vector3 mapBoundsLower;

		public static Vector3 mapBoundsUpper;

		public static Vector3 mapCenter;

		public static Vector3 mapSize;

		public static float DespawnEnemyHeight()
		{
			return mapBoundsLower.y - 10f;
		}
	}
}
