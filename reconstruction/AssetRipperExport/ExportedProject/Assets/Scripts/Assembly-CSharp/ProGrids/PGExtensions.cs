using UnityEngine;

namespace ProGrids
{
	public static class PGExtensions
	{
		public static bool Contains(this Transform[] t_arr, Transform t)
		{
			return false;
		}

		public static float Sum(this Vector3 v)
		{
			return 0f;
		}

		public static bool InFrustum(this Camera cam, Vector3 point)
		{
			return false;
		}
	}
}
