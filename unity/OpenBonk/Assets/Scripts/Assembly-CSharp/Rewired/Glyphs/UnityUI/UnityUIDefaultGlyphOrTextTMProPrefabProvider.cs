#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using UnityEngine;

namespace Rewired.Glyphs.UnityUI
{
	internal class UnityUIDefaultGlyphOrTextTMProPrefabProvider
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Initialize()
		{
		}

		private static GameObject CreateDefaultGlyphOrTextPrefab()
		{
			return null;
		}
	}
}

#endif
