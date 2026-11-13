#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Rewired.Interfaces;
using UnityEngine;

namespace Rewired.Glyphs
{
	public class GlyphProvider : MonoBehaviour, IGlyphProvider
	{
		[SerializeField]
		[Tooltip("Determines if glyphs should be fetched immediately in bulk when available. If false, glyphs will be fetched when queried.")]
		private bool _prefetch;

		[SerializeField]
		[Tooltip("A list of glyph set collections. At least one collection must be assigned.")]
		private List<GlyphSetCollection> _glyphSetCollections;

		[NonSerialized]
		private readonly Dictionary<string, object> _glyphs;

		[NonSerialized]
		private bool _initialized;

		public bool prefetch
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public List<GlyphSetCollection> glyphSetCollections
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected Dictionary<string, object> glyphs => null;

		protected virtual void OnEnable()
		{
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void Update()
		{
		}

		protected virtual void TrySetGlyphProvider()
		{
		}

		protected virtual bool Initialize()
		{
			return false;
		}

		public void Reload()
		{
		}

		bool IGlyphProvider.TryGetGlyph(string key, out object result)
		{
			result = null;
			return false;
		}
	}
}

#endif
