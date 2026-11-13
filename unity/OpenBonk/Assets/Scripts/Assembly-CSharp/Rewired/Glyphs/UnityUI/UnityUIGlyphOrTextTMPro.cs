#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rewired.Glyphs.UnityUI
{
	public class UnityUIGlyphOrTextTMPro : GlyphOrTextBase<Image, Sprite, TMP_Text>
	{
		protected override string textString
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected override Sprite glyphGraphic
		{
			get
			{
				return null;
			}
			set
			{
			}
		}
	}
}

#endif
