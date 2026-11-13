#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewired.Glyphs
{
	public abstract class ControllerElementGlyphBase : MonoBehaviour
	{
		protected class GlyphOrTextObject
		{
			private GlyphOrTextBase _glyphOrText;

			private int _frame;

			private bool _isVisible;

			public virtual bool isVisible
			{
				get
				{
					return false;
				}
				protected set
				{
				}
			}

			public GlyphOrTextBase glyphOrText
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public GlyphOrTextObject(GlyphOrTextBase glyphOrText)
			{
			}

			public virtual void ShowGlyph(object glyph)
			{
			}

			public virtual void ShowText(string text)
			{
			}

			public virtual void Hide()
			{
			}

			public virtual void HideIfIdle()
			{
			}

			public virtual void Destroy()
			{
			}
		}

		public enum AllowedTypes
		{
			All = 0,
			Glyphs = 1,
			Text = 2
		}

		[Tooltip("If set, when glyph/text objects are created, they will be instantiated from this prefab. If left blank, the global default prefab will be used.")]
		[SerializeField]
		private GameObject _glyphOrTextPrefab;

		[Tooltip("Determines what types of objects are allowed.")]
		[SerializeField]
		private AllowedTypes _allowedTypes;

		[NonSerialized]
		private readonly List<GlyphOrTextObject> _entries;

		[NonSerialized]
		private List<object> _tempGlyphs;

		[NonSerialized]
		private GameObject _lastGlyphOrTextPrefab;

		public virtual GameObject glyphOrTextPrefab
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual AllowedTypes allowedTypes
		{
			get
			{
				return default(AllowedTypes);
			}
			set
			{
			}
		}

		protected List<GlyphOrTextObject> entries => null;

		protected virtual void Awake()
		{
		}

		protected virtual void Start()
		{
		}

		protected virtual void OnDestroy()
		{
		}

		protected virtual void OnEnable()
		{
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void Update()
		{
		}

		public virtual void RequireRebuild()
		{
		}

		protected virtual void ClearObjects()
		{
		}

		protected virtual void EvaluateObjectVisibility()
		{
		}

		protected virtual void EvaluateObjectVisibility(Transform transform)
		{
		}

		protected virtual void EvaluateObjectVisibility(Transform transform, List<GlyphOrTextObject> entries)
		{
		}

		protected virtual int ShowGlyphsOrText(ActionElementMap actionElementMap, Transform parent, List<GlyphOrTextObject> entries)
		{
			return 0;
		}

		protected virtual int ShowGlyphsOrText(ActionElementMap actionElementMap)
		{
			return 0;
		}

		protected virtual int ShowGlyphsOrText(ControllerElementIdentifier elementIdentifier, AxisRange axisRange, Transform parent, List<GlyphOrTextObject> entries)
		{
			return 0;
		}

		protected virtual int ShowGlyphsOrText(ControllerElementIdentifier elementIdentifier, AxisRange axisRange)
		{
			return 0;
		}

		protected virtual void Hide()
		{
		}

		protected virtual GameObject GetGlyphOrTextPrefabOrDefault()
		{
			return null;
		}

		protected abstract GameObject GetDefaultGlyphOrTextPrefab();

		protected virtual bool CreateObjectsAsNeeded(Transform parent, List<GlyphOrTextObject> entries, int count)
		{
			return false;
		}

		protected virtual bool IsAllowed(AllowedTypes allowedType)
		{
			return false;
		}

		protected static int GetGlyphs(ActionElementMap actionElementMap, List<object> results)
		{
			return 0;
		}
	}
}

#endif
