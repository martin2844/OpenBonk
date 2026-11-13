#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Rewired.Utils.Interfaces;
using UnityEngine;

namespace Rewired.Utils
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class ExternalTools : IExternalTools
	{
		private static Func<object> _getPlatformInitializerDelegate;

		private bool _isEditorPaused;

		private Action<bool> _EditorPausedStateChangedEvent;

		public static Func<object> getPlatformInitializerDelegate
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public bool isEditorPaused => false;

		public bool UnityInput_IsTouchPressureSupported => false;

		public event Action<bool> EditorPausedStateChangedEvent
		{
			add
			{
			}
			remove
			{
			}
		}

		public event Action<uint, bool> XboxOneInput_OnGamepadStateChange
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public void Destroy()
		{
		}

		public object GetPlatformInitializer()
		{
			return null;
		}

		public string GetFocusedEditorWindowTitle()
		{
			return null;
		}

		public bool IsEditorSceneViewFocused()
		{
			return false;
		}

		public bool LinuxInput_IsJoystickPreconfigured(string name)
		{
			return false;
		}

		public int XboxOneInput_GetUserIdForGamepad(uint id)
		{
			return 0;
		}

		public ulong XboxOneInput_GetControllerId(uint unityJoystickId)
		{
			return 0uL;
		}

		public bool XboxOneInput_IsGamepadActive(uint unityJoystickId)
		{
			return false;
		}

		public string XboxOneInput_GetControllerType(ulong xboxControllerId)
		{
			return null;
		}

		public uint XboxOneInput_GetJoystickId(ulong xboxControllerId)
		{
			return 0u;
		}

		public void XboxOne_Gamepad_UpdatePlugin()
		{
		}

		public bool XboxOne_Gamepad_SetGamepadVibration(ulong xboxOneJoystickId, float leftMotor, float rightMotor, float leftTriggerLevel, float rightTriggerLevel)
		{
			return false;
		}

		public void XboxOne_Gamepad_PulseVibrateMotor(ulong xboxOneJoystickId, int motorInt, float startLevel, float endLevel, ulong durationMS)
		{
		}

		public void GetDeviceVIDPIDs(out List<int> vids, out List<int> pids)
		{
			vids = null;
			pids = null;
		}

		public int GetAndroidAPILevel()
		{
			return 0;
		}

		public void WindowsStandalone_ForwardRawInput(IntPtr rawInputHeaderIndices, IntPtr rawInputDataIndices, uint indicesCount, IntPtr rawInputData, uint rawInputDataSize)
		{
		}

		public bool UnityUI_Graphic_GetRaycastTarget(object graphic)
		{
			return false;
		}

		public void UnityUI_Graphic_SetRaycastTarget(object graphic, bool value)
		{
		}

		public float UnityInput_GetTouchPressure(ref Touch touch)
		{
			return 0f;
		}

		public float UnityInput_GetTouchMaximumPossiblePressure(ref Touch touch)
		{
			return 0f;
		}

		public IControllerTemplate CreateControllerTemplate(Guid typeGuid, object payload)
		{
			return null;
		}

		public Type[] GetControllerTemplateTypes()
		{
			return null;
		}

		public Type[] GetControllerTemplateInterfaceTypes()
		{
			return null;
		}
	}
}

#endif
