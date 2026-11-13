using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProGrids
{
	public static class pg_Util
	{
		private abstract class SnapEnabledOverride
		{
			public abstract bool IsEnabled();
		}

		private class SnapIsEnabledOverride : SnapEnabledOverride
		{
			private bool m_SnapIsEnabled;

			public SnapIsEnabledOverride(bool snapIsEnabled)
			{
			}

			public override bool IsEnabled()
			{
				return false;
			}
		}

		private class ConditionalSnapOverride : SnapEnabledOverride
		{
			public Func<bool> m_IsEnabledDelegate;

			public ConditionalSnapOverride(Func<bool> d)
			{
			}

			public override bool IsEnabled()
			{
				return false;
			}
		}

		private const float EPSILON = 0.0001f;

		private static Dictionary<Transform, SnapEnabledOverride> m_SnapOverrideCache;

		private static Dictionary<Type, bool> m_NoSnapAttributeTypeCache;

		private static Dictionary<Type, MethodInfo> m_ConditionalSnapAttributeCache;

		public static Color ColorWithString(string value)
		{
			return default(Color);
		}

		private static Vector3 VectorToMask(Vector3 vec)
		{
			return default(Vector3);
		}

		private static Axis MaskToAxis(Vector3 vec)
		{
			return default(Axis);
		}

		private static Axis BestAxis(Vector3 vec)
		{
			return default(Axis);
		}

		public static Axis CalcDragAxis(Vector3 movement, Camera cam)
		{
			return default(Axis);
		}

		public static float ValueFromMask(Vector3 val, Vector3 mask)
		{
			return 0f;
		}

		public static Vector3 SnapValue(Vector3 val, float snapValue)
		{
			return default(Vector3);
		}

		private static Type GetType(string type, string assembly = null)
		{
			return null;
		}

		public static void SetUnityGridEnabled(bool isEnabled)
		{
		}

		public static bool GetUnityGridEnabled()
		{
			return false;
		}

		public static Vector3 SnapValue(Vector3 val, Vector3 mask, float snapValue)
		{
			return default(Vector3);
		}

		public static Vector3 SnapToCeil(Vector3 val, Vector3 mask, float snapValue)
		{
			return default(Vector3);
		}

		public static Vector3 SnapToFloor(Vector3 val, float snapValue)
		{
			return default(Vector3);
		}

		public static Vector3 SnapToFloor(Vector3 val, Vector3 mask, float snapValue)
		{
			return default(Vector3);
		}

		public static float Snap(float val, float round)
		{
			return 0f;
		}

		public static float SnapToFloor(float val, float snapValue)
		{
			return 0f;
		}

		public static float SnapToCeil(float val, float snapValue)
		{
			return 0f;
		}

		public static Vector3 CeilFloor(Vector3 v)
		{
			return default(Vector3);
		}

		public static void ClearSnapEnabledCache()
		{
		}

		public static bool SnapIsEnabled(Transform t)
		{
			return false;
		}
	}
}
