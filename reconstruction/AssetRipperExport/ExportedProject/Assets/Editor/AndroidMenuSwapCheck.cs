using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class AndroidMenuSwapCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("AndroidMenuSwapCheck: " + message); }
    public static void Run()
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var query = typeof(AndroidNativeInput).GetMethod("IsButtonPressed", flags);
        var menu = typeof(AndroidNativeInput).GetMethod("MenuButtonPressed", flags);
        var singleton = typeof(SaveManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        var previous = SaveManager.Instance;
        string[] actions = { "Jump", "Interact", "Slide", "Aim", "QuickReset", "MapOverlay" };
        const string prefix = "megabonk.android.gamepad.";
        var present = new bool[actions.Length]; var values = new int[actions.Length];
        for (int i = 0; i < actions.Length; i++) { present[i] = PlayerPrefs.HasKey(prefix + actions[i]); values[i] = PlayerPrefs.GetInt(prefix + actions[i], -1); }
        var root = new GameObject("menu swap test"); root.SetActive(false);
        var saves = root.AddComponent<SaveManager>(); saves.config = new ConfigSaveFile();
        singleton.GetSetMethod(true).Invoke(null, new object[] { saves });
        bool Press(string action, KeyCode key) { Func<KeyCode, bool> f = k => k == key; return (bool)query.Invoke(null, new object[] { action, f, f }); }
        bool Ui(bool submit, bool swap, KeyCode key) { Func<KeyCode, bool> f = k => k == key; return (bool)menu.Invoke(null, new object[] { submit, swap, f }); }
        try
        {
            AndroidControllerBindings.Reset();
            Check(Press("UISubmit", KeyCode.JoystickButton0) && Press("UICancel", KeyCode.JoystickButton1), "default A accepts and B cancels");
            saves.config.cfControlSettings.swap_menu_ab = 1;
            Check(Press("UISubmit", KeyCode.JoystickButton1) && !Press("UISubmit", KeyCode.JoystickButton0), "B accepts in game UI");
            Check(Press("UICancel", KeyCode.JoystickButton0) && !Press("UICancel", KeyCode.JoystickButton1), "A cancels in game UI");
            Check(Ui(true, true, KeyCode.JoystickButton1) && !Ui(true, true, KeyCode.JoystickButton0) && Ui(false, true, KeyCode.JoystickButton0) && !Ui(false, true, KeyCode.JoystickButton1), "Unity module button routing");
            Check(Press("UISubmit", KeyCode.JoystickButton2) && Ui(true, true, KeyCode.JoystickButton2) && Ui(true, true, KeyCode.Return) && Ui(false, true, KeyCode.Escape), "X and keyboard remain usable");
            Check(Press("Jump", KeyCode.JoystickButton0) && Press("Slide", KeyCode.JoystickButton1), "gameplay mappings remain independent");
            Check(AndroidControllerBindings.Set("Jump", 1), "swap jump onto B");
            Check(Press("Jump", KeyCode.JoystickButton1) && !Press("Jump", KeyCode.JoystickButton0) && Press("Slide", KeyCode.JoystickButton0) && !Press("Slide", KeyCode.JoystickButton1), "gameplay A/B actions swapped");
            saves.config.cfControlSettings.swap_menu_ab = 0;
            Check(Press("UISubmit", KeyCode.JoystickButton0) && Press("UICancel", KeyCode.JoystickButton1) && Press("Jump", KeyCode.JoystickButton1), "gameplay swap does not change default menu");
            saves.config.cfControlSettings.swap_menu_ab = 1;
            Check(Press("UISubmit", KeyCode.JoystickButton1) && Press("UICancel", KeyCode.JoystickButton0) && Press("Jump", KeyCode.JoystickButton1), "menu swap remains independent of gameplay swap");
            typeof(AndroidControllerBindings).GetField("loaded", flags).SetValue(null, false);
            ((System.Collections.IDictionary)typeof(AndroidControllerBindings).GetField("Overrides", flags).GetValue(null)).Clear();
            Check(Press("Jump", KeyCode.JoystickButton1) && Press("Slide", KeyCode.JoystickButton0), "gameplay swap reloads from preferences");
            var restored = JsonConvert.DeserializeObject<ConfigSaveFile>(JsonConvert.SerializeObject(saves.config));
            Check(restored.cfControlSettings.swap_menu_ab == 1, "setting survives config serialization");
            Check(new CFControlSettings().swap_menu_ab == 0, "default remains unswapped");
            Debug.Log("AndroidMenuSwapCheck PASS");
        }
        finally {
            for (int i = 0; i < actions.Length; i++) { if (present[i]) PlayerPrefs.SetInt(prefix + actions[i], values[i]); else PlayerPrefs.DeleteKey(prefix + actions[i]); }
            PlayerPrefs.Save();
            ((System.Collections.IDictionary)typeof(AndroidControllerBindings).GetField("Overrides", flags).GetValue(null)).Clear();
            typeof(AndroidControllerBindings).GetField("loaded", flags).SetValue(null, false);
            singleton.GetSetMethod(true).Invoke(null, new object[] { previous }); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
