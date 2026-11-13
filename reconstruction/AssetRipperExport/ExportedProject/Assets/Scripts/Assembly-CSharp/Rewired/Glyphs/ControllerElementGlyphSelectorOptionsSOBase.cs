#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using UnityEngine;

namespace Rewired.Glyphs
{
	[Serializable]
	public abstract class ControllerElementGlyphSelectorOptionsSOBase : ScriptableObject
	{
		public abstract ControllerElementGlyphSelectorOptions options { get; }
	}
}

#endif
