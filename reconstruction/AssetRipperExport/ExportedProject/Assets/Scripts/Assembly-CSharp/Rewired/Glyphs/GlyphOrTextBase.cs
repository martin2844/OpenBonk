#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using UnityEngine;

namespace Rewired.Glyphs
{
	public abstract class GlyphOrTextBase : MonoBehaviour
	{
		[Flags]
		protected enum TypeFlags
		{
			None = 0,
			Glyph = 1,
			Text = 2,
			All = -1
		}

		protected abstract string textString { get; set; }

		public abstract void ShowText(string text);

		public abstract void ShowGlyph(object glyph);

		public virtual void Hide()
		{
		}

		protected abstract void Hide(TypeFlags flags);
	}
	public abstract class GlyphOrTextBase<TGlyphComponent, TGlyphGraphic, TTextComponent> : GlyphOrTextBase where TGlyphComponent : Behaviour where TGlyphGraphic : class where TTextComponent : Behaviour
	{
		[SerializeField]
		private TTextComponent _textComponent;

		[SerializeField]
		private TGlyphComponent _glyphComponent;

		public TTextComponent textComponent
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public TGlyphComponent glyphComponent
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected abstract TGlyphGraphic glyphGraphic { get; set; }

		public override void ShowText(string text)
		{
		}

		public override void ShowGlyph(object glyph)
		{
		}

		public virtual void ShowGlyph(TGlyphGraphic glyph)
		{
		}

		protected override void Hide(TypeFlags flags)
		{
		}
	}
}

#endif
