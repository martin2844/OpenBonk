#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewired.Glyphs
{
	[Serializable]
	public class SpriteGlyphSet : GlyphSet
	{
		[Serializable]
		public class Entry : EntryBase<Sprite>
		{
		}

		[Tooltip("The list of glyphs.")]
		[SerializeField]
		private List<Entry> _glyphs;

		public List<Entry> glyphs
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public override int glyphCount => 0;

		public override EntryBase GetEntry(int index)
		{
			return null;
		}
	}
}

#endif
