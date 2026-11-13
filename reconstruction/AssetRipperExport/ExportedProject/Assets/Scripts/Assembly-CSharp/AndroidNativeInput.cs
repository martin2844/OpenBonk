using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Native Unity Input Manager fallback for builds where the Rewired runtime
/// cannot provide a player. Axis names and the default keyboard bindings come
/// from the recovered MainMenu Rewired maps and ProjectSettings/InputManager.
/// </summary>
public static class AndroidNativeInput
{
	internal static bool SwapMenuButtons => SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfControlSettings != null && SaveManager.Instance.config.cfControlSettings.swap_menu_ab != 0;

	internal static bool MenuButtonPressed(bool submit, bool swap, Func<KeyCode, bool> query)
	{
		if (submit) return query(KeyCode.E) || query(KeyCode.Space) || query(KeyCode.Return) || query(KeyCode.KeypadEnter) || query(swap ? KeyCode.JoystickButton1 : KeyCode.JoystickButton0) || query(KeyCode.JoystickButton2);
		return query(KeyCode.Escape) || query(swap ? KeyCode.JoystickButton0 : KeyCode.JoystickButton1);
	}
	private const string HorizontalAxis = "Horizontal";
	private const string ChestHorizontalAxis = "NativeChestHorizontal";
	private static bool restrictChestHorizontal;
	// Rewired's observed Xbox profile applies a circular dead zone before
	// scaling both axes together. Keep keyboard axes separate from the stick.
	internal static Vector2 ApplyMoveStickDeadZone(Vector2 raw)
	{
		float magnitude = raw.magnitude;
		const float deadZone = .25f;
		if (magnitude <= deadZone) return Vector2.zero;
		float scale = (magnitude - deadZone) / ((1f - deadZone) * magnitude);
		return new Vector2(Mathf.Clamp(raw.x * scale, -1f, 1f), Mathf.Clamp(raw.y * scale, -1f, 1f));
	}
	internal static float CombineMoveAxis(float keyboard, float stick) => Mathf.Abs(keyboard) >= Mathf.Abs(stick) ? keyboard : stick;
	private static Vector2 MoveStick()
	{
		return ApplyMoveStickDeadZone(new Vector2(Input.GetAxisRaw("NativeStickMoveHorizontal"), Input.GetAxisRaw("NativeStickMoveVertical")));
	}

	internal static void RefreshChestHorizontalNavigation(bool chestOpen, bool allowAD)
	{
		restrictChestHorizontal = chestOpen && !allowAD;
		var eventSystem = EventSystem.current;
		var module = eventSystem != null ? eventSystem.GetComponent<StandaloneInputModule>() : null;
		if (module != null) module.horizontalAxis = restrictChestHorizontal ? ChestHorizontalAxis : HorizontalAxis;
	}
	// Sample both physical representations together: some Android pads emit
	// a trigger button as well as an axis, at different points in the travel.
	private sealed class TriggerState
	{
		public bool held, down, up;
		private bool analogHeld;
		public void Sample(bool digital, float analog)
		{
			analogHeld = analogHeld ? analog > 0.4f : analog >= 0.5f;
			bool next = digital || analogHeld;
			down = next && !held;
			up = !next && held;
			held = next;
		}
	}

	private static readonly TriggerState aimTrigger = new TriggerState();
	private static readonly TriggerState mapTrigger = new TriggerState();
	private static readonly TriggerState jumpButton = new TriggerState();
	private static readonly TriggerState interactButton = new TriggerState();
	private static readonly TriggerState slideButton = new TriggerState();
	private static readonly TriggerState resetButton = new TriggerState();
	private static readonly TriggerState wallrunButton = new TriggerState();

	private static float BoundAnalog(string action, float left, float right)
	{
		if (AndroidControllerBindings.TryGet(action, out int button)) return button == 6 ? left : button == 7 ? right : 0f;
		return Matches(action, "Aim") ? left : Matches(action, "MapOverlay") ? right : 0f;
	}

	internal static void SampleTriggers()
	{
		AndroidTouchInput.Sample();
		float left = Input.GetAxisRaw("NativeAimTrigger"), right = Input.GetAxisRaw("NativeMapTrigger");
		SampleAction(aimTrigger, "Aim", left, right);
		SampleAction(mapTrigger, "MapOverlay", left, right);
		SampleAction(jumpButton, "Jump", left, right);
		SampleAction(interactButton, "Interact", left, right);
		SampleAction(slideButton, "Slide", left, right);
		SampleAction(resetButton, "QuickReset", left, right);
		SampleAction(wallrunButton, "Wallrun", left, right);
	}

	private static void SampleAction(TriggerState state, string action, float left, float right)
	{
		bool wasHeld = state.held;
		state.Sample(IsButtonPressed(action, Input.GetKey, Input.GetKey) || AndroidTouchInput.Held(action), BoundAnalog(action, left, right));
		// Preserve a tap that began and ended between two input samples.
		if (!wasHeld && !state.held && AndroidTouchInput.Down(action) && AndroidTouchInput.Up(action))
			state.down = state.up = true;
	}

	private static TriggerState SampledAction(string action)
	{
		if (Matches(action, "Aim")) return aimTrigger;
		if (Matches(action, "MapOverlay")) return mapTrigger;
		if (Matches(action, "Jump", "Jump Bhop")) return jumpButton;
		if (Matches(action, "Interact")) return interactButton;
		if (Matches(action, "Slide")) return slideButton;
		if (Matches(action, "QuickReset")) return resetButton;
		if (Matches(action, "Wallrun")) return wallrunButton;
		return null;
	}
	public static float GetAxis(string action)
	{
		if (Matches(action, "Move Horizontal"))
		{
			return CombineMoveAxis(CombineMoveAxis(Input.GetAxis("NativeMoveHorizontal"), MoveStick().x), AndroidTouchInput.Move.x);
		}
		if (Matches(action, "UIHorizontal"))
		{
			#if UNITY_ANDROID && !UNITY_EDITOR
			return Input.GetAxisRaw(restrictChestHorizontal ? ChestHorizontalAxis : HorizontalAxis);
			#else
			return Input.GetAxis(restrictChestHorizontal ? ChestHorizontalAxis : HorizontalAxis);
			#endif
		}
		if (Matches(action, "Move Vertical"))
		{
			return CombineMoveAxis(CombineMoveAxis(Input.GetAxis("NativeMoveVertical"), MoveStick().y), AndroidTouchInput.Move.y);
		}
		if (Matches(action, "UIVertical"))
		{
			return Input.GetAxis("Vertical");
		}
		if (Matches(action, "Look Horizontal"))
		{
			return CombineMoveAxis(Input.GetAxis("ControllerLookHorizontal"), AndroidTouchInput.Look.x);
		}
		if (Matches(action, "Look Vertical"))
		{
			return CombineMoveAxis(Input.GetAxis("ControllerLookVertical"), -AndroidTouchInput.Look.y);
		}
		return 0f;
	}

	public static bool GetButton(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidControllerBindings.IsListening && Matches(action, "UISubmit", "UICancel", "UIAbort", "UIShoulderLeft", "UIShoulderRight")) return false;
		if (Matches(action, "UISubmit") && Input.GetKey(KeyCode.JoystickButton10)) return false;
		var sampled = SampledAction(action); if (sampled != null) return sampled.held;
#endif
		return IsButtonPressed(action, Input.GetKey, Input.GetKey);
	}

	public static bool GetButtonDown(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidControllerBindings.IsListening && Matches(action, "UISubmit", "UICancel", "UIAbort", "UIShoulderLeft", "UIShoulderRight")) return false;
		if (Matches(action, "UISubmit") && Input.GetKeyDown(KeyCode.JoystickButton10)) return false;
		var sampled = SampledAction(action); if (sampled != null) return sampled.down;
#endif
		return IsButtonPressed(action, Input.GetKeyDown, Input.GetKeyDown);
	}

	public static bool GetButtonUp(string action)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (AndroidControllerBindings.IsListening && Matches(action, "UISubmit", "UICancel", "UIAbort", "UIShoulderLeft", "UIShoulderRight")) return false;
		if (Matches(action, "UISubmit") && Input.GetKeyUp(KeyCode.JoystickButton10)) return false;
		var sampled = SampledAction(action); if (sampled != null) return sampled.up;
#endif
		return IsButtonPressed(action, Input.GetKeyUp, Input.GetKeyUp);
	}

	private static bool IsButtonPressed(string action, Func<KeyCode, bool> keyQuery, Func<KeyCode, bool> joystickQuery)
	{
		if (string.IsNullOrEmpty(action))
		{
			return false;
		}

		if (Matches(action, "Jump", "Jump Bhop"))
		{
			return keyQuery(KeyCode.Space) || Gamepad(action, joystickQuery, KeyCode.JoystickButton0, KeyCode.JoystickButton5);
		}
		if (Matches(action, "UISubmit"))
		{
			// Match the Submit axis used by Unity's StandaloneInputModule. Some
			// menu buttons also read this action directly instead of OnSubmit.
			return keyQuery(KeyCode.E) || keyQuery(KeyCode.Space) || keyQuery(KeyCode.Return) || keyQuery(KeyCode.KeypadEnter) || joystickQuery(SwapMenuButtons ? KeyCode.JoystickButton1 : KeyCode.JoystickButton0) || joystickQuery(KeyCode.JoystickButton2);
		}
		if (Matches(action, "Interact"))
		{
			return keyQuery(KeyCode.E) || Gamepad(action, joystickQuery, KeyCode.JoystickButton2);
		}
		if (Matches(action, "Slide"))
		{
			return keyQuery(KeyCode.LeftControl) || Gamepad(action, joystickQuery, KeyCode.JoystickButton1, KeyCode.JoystickButton4);
		}
		if (Matches(action, "Aim"))
		{
			return keyQuery(KeyCode.Mouse1) || Gamepad(action, joystickQuery, KeyCode.JoystickButton3, KeyCode.JoystickButton6);
		}
		if (Matches(action, "Ability"))
		{
			return keyQuery(KeyCode.LeftShift);
		}
		if (Matches(action, "QuickReset"))
		{
			return keyQuery(KeyCode.R) || Gamepad(action, joystickQuery);
		}
		if (Matches(action, "Wallrun")) return Gamepad(action, joystickQuery);
		if (Matches(action, "MapOverlay"))
		{
			return keyQuery(KeyCode.Tab) || Gamepad(action, joystickQuery, KeyCode.JoystickButton7);
		}
		if (Matches(action, "UICancel"))
		{
			return keyQuery(KeyCode.Escape) || joystickQuery(SwapMenuButtons ? KeyCode.JoystickButton0 : KeyCode.JoystickButton1);
		}
		if (Matches(action, "UIAbort"))
		{
			return keyQuery(KeyCode.Escape) || joystickQuery(KeyCode.JoystickButton10);
		}
		if (Matches(action, "UIShoulderLeft"))
		{
			return joystickQuery(KeyCode.JoystickButton4);
		}
		if (Matches(action, "UIShoulderRight"))
		{
			return joystickQuery(KeyCode.JoystickButton5);
		}
		return false;
	}

	private static bool Gamepad(string action, Func<KeyCode, bool> query, params KeyCode[] defaults)
	{
		if (AndroidControllerBindings.TryGet(action, out int button)) return button >= 0 && query((KeyCode)((int)KeyCode.JoystickButton0 + button));
		foreach (KeyCode key in defaults) if (query(key)) return true;
		return false;
	}

	private static bool Matches(string action, params string[] names)
	{
		if (action == null)
		{
			return false;
		}
		for (int i = 0; i < names.Length; i++)
		{
			if (string.Equals(action, names[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}

#if UNITY_ANDROID && !UNITY_EDITOR
[DefaultExecutionOrder(-32000)]
internal sealed class AndroidNativeTriggerSampler : MonoBehaviour
{
	private EventSystem lastEventSystem;
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Install()
	{
		var go = new GameObject("Android native trigger input");
		DontDestroyOnLoad(go);
		go.AddComponent<AndroidNativeTriggerSampler>();
	}

	private void Update()
	{
		AndroidNativeInput.SampleTriggers();
		var events = EventSystem.current;
		if (events == null) { lastEventSystem = null; return; }
		var module = events.GetComponent<StandaloneInputModule>();
		if (module == null) return;
		if (events != lastEventSystem || module.inputOverride == null)
		{
			var input = events.GetComponent<AndroidUiInputOverride>();
			if (input == null) input = events.gameObject.AddComponent<AndroidUiInputOverride>();
			module.inputOverride = input;
			lastEventSystem = events;
		}
	}
}

// Android can report a controller Start press as both joystick button 10 and
// Return. Start is the pause action; do not also submit the focused UI button.
internal sealed class AndroidUiInputOverride : BaseInput
{
	public override bool GetButtonDown(string buttonName)
	{
		if (string.Equals(buttonName, "Submit", StringComparison.OrdinalIgnoreCase) && Input.GetKeyDown(KeyCode.JoystickButton10)) return false;
		if (AndroidNativeInput.SwapMenuButtons)
		{
			if (string.Equals(buttonName, "Submit", StringComparison.OrdinalIgnoreCase)) return AndroidNativeInput.MenuButtonPressed(true, true, Input.GetKeyDown);
			if (string.Equals(buttonName, "Cancel", StringComparison.OrdinalIgnoreCase)) return AndroidNativeInput.MenuButtonPressed(false, true, Input.GetKeyDown);
		}
		return base.GetButtonDown(buttonName);
	}
}
#endif
