#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using UnityEngine;

namespace Rewired.Glyphs
{
	[Serializable]
	public class ControllerElementGlyphSelectorOptions
	{
		[Tooltip("Determines if the Player's last active controller is used for glyph selection.")]
		[SerializeField]
		private bool _useLastActiveController;

		[Tooltip("List of controller type priority. First in list corresponds to highest priority. This determines which controller types take precedence when displaying glyphs. If use last active controller is enabled, the active controller will always take priority, however, if there is no last active controller, selection will fall back based on this priority. In addition, keyboard and mouse are treated as a single controller for the purposes of glyph handling, so to prioritze keyboard over mouse or vice versa, the one that is lower in the list will take precedence.")]
		[SerializeField]
		private ControllerType[] _controllerTypeOrder;

		private static ControllerElementGlyphSelectorOptions s_defaultOptions;

		public bool useLastActiveController
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public ControllerType[] controllerTypeOrder
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public static ControllerElementGlyphSelectorOptions defaultOptions
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual bool TryGetControllerTypeOrder(int index, out ControllerType controllerType)
		{
			controllerType = default(ControllerType);
			return false;
		}
	}
}

#endif
