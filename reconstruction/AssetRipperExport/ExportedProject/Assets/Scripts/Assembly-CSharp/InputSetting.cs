using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputSetting : BetterSetting
{
	public KeyDisplay[] keyDisplays;

	private int listenSlot;

	private new void Awake()
	{
	}

	private new void OnDestroy()
	{
		if (nativeSubscribed) KeyListener.A_MapChanged -= ShowValue;
		EndNativeListening();
		base.OnDestroy();
	}

	private EventSystem nativeEventSystem;
	private bool previousNavigation;
	private bool nativeListening;
	private bool nativeSubscribed;
	private int nativeListenGeneration;

	private bool IsNativeControllerRow => _settingName != null && _settingName.StartsWith("controller_") && _settingValue is string;

	private void OnEnable()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (!nativeListening) ShowValue();
#endif
	}

	private void OnDisable()
	{
		EndNativeListening();
	}

	protected override void OnSetting()
	{
		if (!nativeSubscribed) { KeyListener.A_MapChanged += ShowValue; nativeSubscribed = true; }
	}

	private void OnControllerChange(EControllerType controllerType)
	{
	}

	internal static void SetNativeLabel(KeyDisplay display, string label)
	{
		if (display == null) return;
		if (display.glyph != null) display.glyph.gameObject.SetActive(false);
		// The original keycap background is only wide enough for one keyboard key.
		// Native gamepad bindings can show several buttons or a listening prompt.
		if (display.background != null) display.background.enabled = false;
		if (display.text == null) return;
		display.text.gameObject.SetActive(true);
		display.text.color = Color.white;
		display.text.text = label;
		if (display.textSizer != null) { display.textSizer.Refresh(); display.textSizer.Recalculate(); }
	}

	public override void ControllerInputDir(int dir, float multiplier)
	{
	}

	protected override void ShowValue()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (keyDisplays == null || keyDisplays.Length == 0 || keyDisplays[0] == null) return;
		SetNativeLabel(keyDisplays[0], IsNativeControllerRow ? AndroidControllerBindings.DisplayForAction((string)_settingValue) : AndroidControlText.Fixed);
#endif
	}

	public void StartListening(int slot)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (!IsNativeControllerRow || nativeListening || AndroidControllerBindings.IsListening) return;
		StartCoroutine(ListenForNativeButton());
#endif
	}

	private IEnumerator ListenForNativeButton()
	{
		int generation = ++nativeListenGeneration;
		nativeListening = true;
		AndroidControllerBindings.IsListening = true;
		nativeEventSystem = EventSystem.current;
		if (nativeEventSystem != null) { previousNavigation = nativeEventSystem.sendNavigationEvents; nativeEventSystem.sendNavigationEvents = false; }
		if (keyDisplays != null && keyDisplays.Length > 0) SetNativeLabel(keyDisplays[0], AndroidControlText.PressButton);
		float ready = Time.unscaledTime + .4f, timeout = Time.unscaledTime + 10f;
		bool triggerReady = false;
		while (generation == nativeListenGeneration && Time.unscaledTime < timeout)
		{
			float left = Input.GetAxisRaw("NativeAimTrigger"), right = Input.GetAxisRaw("NativeMapTrigger");
			if (left < .2f && right < .2f) triggerReady = true;
			if (Time.unscaledTime >= ready)
			{
				if (Input.GetKeyDown(KeyCode.Escape)) break;
				int button = -1;
				for (int i = 0; i < 20; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i))) { button = i; break; }
				if (button < 0 && triggerReady) { if (left >= .65f) button = 6; else if (right >= .65f) button = 7; }
				if (button >= 0)
				{
					AndroidControllerBindings.Set((string)_settingValue, button);
					// Do not deliver the binding press to the currently selected UI button.
					while (generation == nativeListenGeneration && (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + button)) || (button == 6 && Input.GetAxisRaw("NativeAimTrigger") > .4f) || (button == 7 && Input.GetAxisRaw("NativeMapTrigger") > .4f))) yield return null;
					break;
				}
			}
			yield return null;
		}
		if (generation != nativeListenGeneration) yield break;
		yield return null;
		if (generation != nativeListenGeneration) yield break;
		EndNativeListening();
		ShowValue();
	}

	private void EndNativeListening()
	{
		if (!nativeListening) return;
		nativeListening = false;
		nativeListenGeneration++;
		AndroidControllerBindings.IsListening = false;
		if (nativeEventSystem != null) nativeEventSystem.sendNavigationEvents = previousNavigation;
		nativeEventSystem = null;
	}

	public void SetKey(int keyCode)
	{
	}
}
