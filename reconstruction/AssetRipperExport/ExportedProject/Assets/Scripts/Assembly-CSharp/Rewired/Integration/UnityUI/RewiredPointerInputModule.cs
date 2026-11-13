#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System.Collections.Generic;
using Rewired.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rewired.Integration.UnityUI
{
	public abstract class RewiredPointerInputModule : BaseInputModule
	{
		protected class MouseState
		{
			private List<ButtonState> m_TrackedButtons;

			public bool AnyPressesThisFrame()
			{
				return false;
			}

			public bool AnyReleasesThisFrame()
			{
				return false;
			}

			public ButtonState GetButtonState(int button)
			{
				return null;
			}

			public void SetButtonState(int button, PointerEventData.FramePressState stateForMouseButton, PlayerPointerEventData data)
			{
			}
		}

		public class MouseButtonEventData
		{
			public PointerEventData.FramePressState buttonState;

			public PlayerPointerEventData buttonData;

			public bool PressedThisFrame()
			{
				return false;
			}

			public bool ReleasedThisFrame()
			{
				return false;
			}
		}

		protected class ButtonState
		{
			private int m_Button;

			private MouseButtonEventData m_EventData;

			public MouseButtonEventData eventData
			{
				get
				{
					return null;
				}
				set
				{
				}
			}

			public int button
			{
				get
				{
					return 0;
				}
				set
				{
				}
			}
		}

		private sealed class UnityInputSource : IMouseInputSource, ITouchInputSource
		{
			private Vector2 m_MousePosition;

			private Vector2 m_MousePositionPrev;

			private int m_LastUpdatedFrame;

			int IMouseInputSource.playerId => 0;

			int ITouchInputSource.playerId => 0;

			bool IMouseInputSource.enabled => false;

			bool IMouseInputSource.locked => false;

			int IMouseInputSource.buttonCount => 0;

			Vector2 IMouseInputSource.screenPosition => default(Vector2);

			Vector2 IMouseInputSource.screenPositionDelta => default(Vector2);

			Vector2 IMouseInputSource.wheelDelta => default(Vector2);

			bool ITouchInputSource.touchSupported => false;

			int ITouchInputSource.touchCount => 0;

			bool IMouseInputSource.GetButtonDown(int button)
			{
				return false;
			}

			bool IMouseInputSource.GetButtonUp(int button)
			{
				return false;
			}

			bool IMouseInputSource.GetButton(int button)
			{
				return false;
			}

			Touch ITouchInputSource.GetTouch(int index)
			{
				return default(Touch);
			}

			private void TryUpdate()
			{
			}
		}

		public const int kMouseLeftId = -1;

		public const int kMouseRightId = -2;

		public const int kMouseMiddleId = -3;

		public const int kFakeTouchesId = -4;

		private const int customButtonsStartingId = -2147483520;

		private const int customButtonsMaxCount = 128;

		private const int customButtonsLastId = -2147483392;

		private readonly List<IMouseInputSource> m_MouseInputSourcesList;

		private Dictionary<int, Dictionary<int, PlayerPointerEventData>[]> m_PlayerPointerData;

		private ITouchInputSource m_UserDefaultTouchInputSource;

		private UnityInputSource __m_DefaultInputSource;

		private readonly MouseState m_MouseState;

		private UnityInputSource defaultInputSource => null;

		private IMouseInputSource defaultMouseInputSource => null;

		protected ITouchInputSource defaultTouchInputSource => null;

		protected virtual bool isMouseSupported => false;

		protected bool IsDefaultMouse(IMouseInputSource mouse)
		{
			return false;
		}

		public IMouseInputSource GetMouseInputSource(int playerId, int mouseIndex)
		{
			return null;
		}

		public void RemoveMouseInputSource(IMouseInputSource source)
		{
		}

		public void AddMouseInputSource(IMouseInputSource source)
		{
		}

		public int GetMouseInputSourceCount(int playerId)
		{
			return 0;
		}

		public ITouchInputSource GetTouchInputSource(int playerId, int sourceIndex)
		{
			return null;
		}

		public void RemoveTouchInputSource(ITouchInputSource source)
		{
		}

		public void AddTouchInputSource(ITouchInputSource source)
		{
		}

		public int GetTouchInputSourceCount(int playerId)
		{
			return 0;
		}

		protected void ClearMouseInputSources()
		{
		}

		protected abstract bool IsDefaultPlayer(int playerId);

		protected bool GetPointerData(int playerId, int pointerIndex, int pointerTypeId, out PlayerPointerEventData data, bool create, PointerEventType pointerEventType)
		{
			data = null;
			return false;
		}

		private PlayerPointerEventData CreatePointerEventData(int playerId, int pointerIndex, int pointerTypeId, PointerEventType pointerEventType)
		{
			return null;
		}

		protected void RemovePointerData(PlayerPointerEventData data)
		{
		}

		protected PlayerPointerEventData GetTouchPointerEventData(int playerId, int touchDeviceIndex, Touch input, out bool pressed, out bool released)
		{
			pressed = default(bool);
			released = default(bool);
			return null;
		}

		protected virtual MouseState GetMousePointerEventData(int playerId, int mouseIndex)
		{
			return null;
		}

		protected PlayerPointerEventData GetLastPointerEventData(int playerId, int pointerIndex, int pointerTypeId, bool ignorePointerTypeId, PointerEventType pointerEventType)
		{
			return null;
		}

		private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
		{
			return false;
		}

		protected virtual void ProcessMove(PlayerPointerEventData pointerEvent)
		{
		}

		protected virtual void ProcessDrag(PlayerPointerEventData pointerEvent)
		{
		}

		public override bool IsPointerOverGameObject(int pointerTypeId)
		{
			return false;
		}

		protected void ClearSelection()
		{
		}

		public override string ToString()
		{
			return null;
		}

		protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
		{
		}

		protected void CopyFromTo(PointerEventData from, PointerEventData to)
		{
		}

		protected PointerEventData.FramePressState StateForMouseButton(int playerId, int mouseIndex, int buttonId)
		{
			return default(PointerEventData.FramePressState);
		}
	}
}

#endif
