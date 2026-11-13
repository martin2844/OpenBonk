using System;
using System.Reflection;
using UnityEngine;

public static class AndroidInputBindingCheck
{
	public static void Run()
	{
		var method = typeof(AndroidNativeInput).GetMethod("IsButtonPressed", BindingFlags.NonPublic | BindingFlags.Static);
		bool Pressed(string action, KeyCode key) => (bool)method.Invoke(null, new object[] { action, (Func<KeyCode, bool>)(k => k == key), (Func<KeyCode, bool>)(k => k == key) });
		void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
		Check(Pressed("Interact", KeyCode.E), "E must interact");
		Check(!Pressed("Jump", KeyCode.E), "E must not jump");
		Check(!Pressed("Jump", KeyCode.Return), "Return must not jump");
		Check(Pressed("Jump", KeyCode.Space), "Space must jump");
		Check(Pressed("UISubmit", KeyCode.E) && Pressed("UISubmit", KeyCode.Space) && Pressed("UISubmit", KeyCode.Return), "Menu submit keys must remain available");
		Check(Pressed("Jump", KeyCode.JoystickButton0) && Pressed("UISubmit", KeyCode.JoystickButton0), "Gamepad button 0 must still jump and submit");
		Check(Pressed("UISubmit", KeyCode.JoystickButton2) && !Pressed("Jump", KeyCode.JoystickButton2), "X must submit in menus that read the action directly, without jumping");
		Check(Pressed("Jump", KeyCode.JoystickButton5), "Right bumper must also jump");
		Check(Pressed("Interact", KeyCode.JoystickButton2) && !Pressed("Slide", KeyCode.JoystickButton2), "X must interact without sliding");
		Check(Pressed("Slide", KeyCode.JoystickButton1) && Pressed("Slide", KeyCode.JoystickButton4) && !Pressed("Interact", KeyCode.JoystickButton1), "B and left bumper must slide without interacting");
		Check(Pressed("UIAbort", KeyCode.JoystickButton10), "Gamepad Start must open pause");
		Check(!Pressed("UIAbort", KeyCode.JoystickButton3) && Pressed("Aim", KeyCode.JoystickButton3), "Aim must not also open pause");
		Check(Pressed("Aim", KeyCode.JoystickButton6) && !Pressed("MapOverlay", KeyCode.JoystickButton6), "Left trigger must aim without opening the map");
		Check(Pressed("MapOverlay", KeyCode.JoystickButton7) && !Pressed("Aim", KeyCode.JoystickButton7), "Right trigger must open the map without aiming");
		Debug.Log("AndroidInputBindingCheck passed");
	}
}
