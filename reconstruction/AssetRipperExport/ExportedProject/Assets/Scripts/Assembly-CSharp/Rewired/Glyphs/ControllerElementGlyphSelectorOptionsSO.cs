#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using UnityEngine;

namespace Rewired.Glyphs
{
	[Serializable]
	public class ControllerElementGlyphSelectorOptionsSO : ControllerElementGlyphSelectorOptionsSOBase
	{
		[SerializeField]
		private ControllerElementGlyphSelectorOptions _options;

		public override ControllerElementGlyphSelectorOptions options => null;
	}
}

#endif
