#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;

namespace Rewired.Glyphs
{
	public abstract class ControllerElementGlyph : ControllerElementGlyphBase
	{
		[NonSerialized]
		private ActionElementMap _actionElementMap;

		[NonSerialized]
		private ControllerElementIdentifier _controllerElementIdentifier;

		[NonSerialized]
		private AxisRange _axisRange;

		public ActionElementMap actionElementMap
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public ControllerElementIdentifier controllerElementIdentifier
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public AxisRange axisRange
		{
			get
			{
				return default(AxisRange);
			}
			set
			{
			}
		}

		protected override void Update()
		{
		}
	}
}

#endif
