#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System.Collections.Generic;

namespace Rewired.Glyphs
{
	public static class GlyphTools
	{
		public static bool TryGetActionElementMaps(int playerId, int actionId, AxisRange actionRange, ControllerElementGlyphSelectorOptions options, List<ActionElementMap> workingActionElementMaps, out ActionElementMap aemResult1, out ActionElementMap aemResult2)
		{
			aemResult1 = null;
			aemResult2 = null;
			return false;
		}

		public static bool TryGetActionElementMaps(InputAction action, AxisRange actionRange, List<ActionElementMap> tempAems, out ActionElementMap aemResult1, out ActionElementMap aemResult2)
		{
			aemResult1 = null;
			aemResult2 = null;
			return false;
		}

		public static ActionElementMap FindFirstFullAxisBinding(List<ActionElementMap> actionElementMaps)
		{
			return null;
		}

		public static ActionElementMap FindFirstBinding(List<ActionElementMap> actionElementMaps, AxisRange actionRange)
		{
			return null;
		}

		public static bool FindFirstSplitAxisBindingPair(List<ActionElementMap> actionElementMaps, out ActionElementMap negativeAem, out ActionElementMap positiveAem)
		{
			negativeAem = null;
			positiveAem = null;
			return false;
		}

		public static bool IsMousePrioritizedOverKeyboard(ControllerElementGlyphSelectorOptions options)
		{
			return false;
		}

		private static int GetElementMapsWithAction(Player player, ControllerType controllerType, int controllerId, int actionId, bool skipDisabledMaps, List<ActionElementMap> results)
		{
			return 0;
		}

		private static int GetElementMapsWithAction(Player player, ControllerType controllerType, int actionId, bool skipDisabledMaps, List<ActionElementMap> results)
		{
			return 0;
		}

		private static int GetElementMapsWithAction(Player player, int actionId, bool skipDisabledMaps, List<ActionElementMap> results)
		{
			return 0;
		}

		private static int RemoveInvalidElementMaps(Player player, List<ActionElementMap> results, int startIndex)
		{
			return 0;
		}
	}
}

#endif
