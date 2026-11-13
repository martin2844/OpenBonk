using System.Collections.Generic;
using UnityEngine;

namespace UI.Debugging
{
	public class DebugText : MonoBehaviour
	{
		public static Dictionary<string, string> debugEntries;

		public static Dictionary<string, int> debugPriority;

		private static List<string> sortedKeys;

		private void OnGUI()
		{
		}

		public static void DebugValue(string key, string value, int priority = 0)
		{
		}

		private static void SortKeysByPriority()
		{
		}
	}
}
