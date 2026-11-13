#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewired.Glyphs.UnityUI
{
	public abstract class UnityUIPlayerControllerElementGlyphBase : UnityUIControllerElementGlyphBase
	{
		[Tooltip("Optional reference to an object that defines options. If blank, the global default options will be used.")]
		[SerializeField]
		private ControllerElementGlyphSelectorOptionsSOBase _options;

		[Tooltip("The range of the Action for which to show glyphs / text. This determines whether to show the glyph for an axis-type Action (ex: Move Horizontal), or the positive/negative pole of an Action (ex: Move Right). For button-type Actions, Full and Positive are equivalent.")]
		[SerializeField]
		private AxisRange _actionRange;

		[Tooltip("Optional parent Transform of the first group of instantiated glyph / text objects. If an axis-type Action is bound to multiple elements, the glyphs bound to the negative pole of the Action will be instantiated under this Transform. This allows you to separate negative and positive groups in order to stack glyph groups horizontally or vertically, for example. If an Action is only bound to one element, the glyph will be instantiated under this transform. If blank, objects will be created as children of this object's Transform.")]
		[SerializeField]
		private Transform _group1;

		[Tooltip("Optional parent Transform of the second group of instantiated glyph / text objects. If an axis-type Action is bound to multiple elements, the glyphs bound to the positive pole of the Action will be instantiated under this Transform. This allows you to separate negative and positive groups in order to stack glyph groups horizontally or vertically, for example. If an Action is only bound to one element, the glyph will be instantiated under group1 instead. If blank, objects will be created as children of either group1 if set or the object's Transform.")]
		[SerializeField]
		private Transform _group2;

		[NonSerialized]
		private List<ActionElementMap> _tempAems;

		[NonSerialized]
		private List<ActionElementMap> _tempCombinedElementAems;

		[NonSerialized]
		private readonly List<GlyphOrTextObject> _group1Objects;

		[NonSerialized]
		private readonly List<GlyphOrTextObject> _group2Objects;

		public virtual ControllerElementGlyphSelectorOptionsSOBase options
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public abstract int playerId { get; set; }

		public abstract int actionId { get; set; }

		public virtual AxisRange actionRange
		{
			get
			{
				return default(AxisRange);
			}
			set
			{
			}
		}

		public virtual Transform group1
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual Transform group2
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected virtual bool isMousePrioritizedOverKeyboard => false;

		protected virtual bool TryGetControllerTypeOrder(int index, out ControllerType controllerType)
		{
			controllerType = default(ControllerType);
			return false;
		}

		protected override void Update()
		{
		}

		protected override void ClearObjects()
		{
		}

		protected virtual bool ShowBinding(ActionElementMap actionElementMap)
		{
			return false;
		}

		protected virtual bool ShowSplitAxisBindings(ActionElementMap negativeAem, ActionElementMap positiveAem)
		{
			return false;
		}

		protected override void EvaluateObjectVisibility()
		{
		}

		protected virtual int ShowGlyphsOrText(IList<ActionElementMap> bindings, Transform parent, List<GlyphOrTextObject> objects)
		{
			return 0;
		}

		protected override void Hide()
		{
		}

		protected virtual Transform GetObjectGroupTransform(int groupIndex)
		{
			return null;
		}

		protected virtual ControllerElementGlyphSelectorOptions GetOptionsOrDefault()
		{
			return null;
		}
	}
}

#endif
