using System;
using System.Collections.Generic;
#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired;

public class InputSettingNew : BetterSetting
{
	private class Hostage
	{
		public ActionElementMap actionElementMap;

		public Controller controller;

		public int elementIdentifierId;

		public Hostage(ActionElementMap actionElementMap)
		{
		}
	}

	public struct InputKey : IEquatable<InputKey>
	{
		public int elementId;

		public ControllerType controllerType;

		public int controllerId;

		public bool Equals(InputKey other)
		{
			return false;
		}

		public override bool Equals(object obj)
		{
			return false;
		}

		public override int GetHashCode()
		{
			return 0;
		}
	}

	public MyGlyphDisplay[] glyphContainers;

	public MyButtonSetting b_settingButton;

	private Controller currentController;

	private bool isController;

	private int actionId;

	private bool isInitialized;

	private static int selectedIndex;

	private static Dictionary<InputKey, int> elementToGlyphPosition;

	private Hostage hostage;

	private ActionElementMap aemKeyboard;

	private ActionElementMap aemMouse;

	private ActionElementMap aemController;

	public static Action A_MappingUpdated;

	private new void Awake()
	{
	}

	private new void OnDestroy()
	{
	}

	private void OnMappingUpdated()
	{
	}

	private void OnEnable()
	{
	}

	private void CheckController()
	{
	}

	private void StartHover()
	{
	}

	private void StopHover()
	{
	}

	public override void ControllerInputDir(int dir, float multiplier)
	{
	}

	private ControllerMap GetControllerMap()
	{
		return null;
	}

	private ControllerMap GetKeyboardMap()
	{
		return null;
	}

	private ControllerMap GetMouseMap()
	{
		return null;
	}

	public void StartListening()
	{
	}

	public void ForceListen(int index)
	{
	}

	private List<InputMapper.Context> GetContextController()
	{
		return null;
	}

	private List<InputMapper.Context> GetContextKeyboardAndMouse()
	{
		return null;
	}

	private void TryInit()
	{
	}

	protected override void ShowValue()
	{
	}

	private void ShowMapBindings(IList<ControllerMap> maps)
	{
	}

	private bool IsElementMapFilteredOut(ActionElementMap actionElementMap)
	{
		return false;
	}

	private void ModifyContext(InputMapper.Context context)
	{
	}

	private void ClearGlyphs()
	{
	}

	private void RefreshController(Controller c)
	{
	}

	private void OnControlsReset()
	{
	}

	public void UpdateMapping(bool result, ActionElementMap newActionElementMap)
	{
	}

	public void TryRemoveInputKey(ActionElementMap aem)
	{
	}

	private void TryRemoveInputKey(InputKey key)
	{
	}

	private void TryAddInputKey(ActionElementMap aem, int index)
	{
	}

	public InputKey GetNewInputKey(ActionElementMap aem)
	{
		return default(InputKey);
	}

	private void OnInputMapped(InputMapper.InputMappedEventData obj)
	{
	}
}
#else
// The unreconstructed desktop remapping row was already inert on Android.
// Android's functional binding rows use AndroidControllerBindings instead.
public class InputSettingNew : BetterSetting
{
    public MyGlyphDisplay[] glyphContainers;
    public MyButtonSetting b_settingButton;
    public static Action A_MappingUpdated;
    public override void ControllerInputDir(int dir, float multiplier) { }
    public void StartListening() { }
    public void ForceListen(int index) { }
    protected override void ShowValue() { }
}
#endif
