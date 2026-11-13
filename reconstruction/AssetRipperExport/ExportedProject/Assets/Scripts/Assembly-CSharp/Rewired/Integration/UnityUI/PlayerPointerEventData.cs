#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired.UI;
using UnityEngine.EventSystems;

namespace Rewired.Integration.UnityUI
{
	public class PlayerPointerEventData : PointerEventData
	{
		public int playerId { get; set; }

		public int inputSourceIndex { get; set; }

		public IMouseInputSource mouseSource { get; set; }

		public ITouchInputSource touchSource { get; set; }

		public PointerEventType sourceType { get; set; }

		public int buttonIndex { get; set; }

		public PlayerPointerEventData(EventSystem eventSystem)
			: base(null)
		{
		}

		public override string ToString()
		{
			return null;
		}
	}
}

#endif
