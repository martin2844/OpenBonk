#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using UnityEngine;

namespace Rewired.Glyphs.UnityUI
{
	public abstract class UnityUIControllerElementGlyphBase : ControllerElementGlyphBase
	{
		private static GameObject s_defaultGlyphOrTextPrefab;

		private static Func<GameObject> s_defaultGlyphOrTextPrefabProvider;

		public static GameObject defaultGlyphOrTextPrefab
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public static Func<GameObject> defaultGlyphOrTextPrefabProvider
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected override GameObject GetDefaultGlyphOrTextPrefab()
		{
			return null;
		}

		private static GameObject CreateDefaultGlyphOrTextPrefab()
		{
			return null;
		}
	}
}

#endif
