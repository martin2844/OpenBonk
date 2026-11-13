using System;
using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class SettingsSliderPortCheck
{
    sealed class TestSettings : CFSettings { public float fov = 85f; public float fps_limit = 147f; public List<SettingHeader> GetHeaders() => new List<SettingHeader>(); }
    static void Require(bool value, string why) { if (!value) throw new Exception("SettingsSliderPortCheck: " + why); }
    static void Range(string name, float low, float high) { ConfigSettingsUtility.GetSliderRange(name, out float min, out float max); Require(min == low && max == high, name + " range"); }
    public static void Run()
    {
        Range("fov", 60, 140); Range("fps_limit", 30, 300); Range("minimap_size", .5f, 2f); Range("crosshair_size", .25f, 2f); Range("music", 0, 1); Range("sensitivity", 0, 10);
        Require(ConfigSettingsUtility.GetSliderWholeNumbers("fov") && !ConfigSettingsUtility.GetSliderWholeNumbers("sensitivity"), "whole number classification");
        Require(ConfigSettingsUtility.GetSliderIncrement("sensitivity") == .01f && ConfigSettingsUtility.GetSliderIncrement("fov") == -1f, "original controller step overrides");
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SliderSetting.prefab"); Require(asset != null, "original slider prefab");
        var obj = UnityEngine.Object.Instantiate(asset); var slider = obj.GetComponent<SliderSetting>(); Require(slider && slider.slider && slider.valueText && slider.settingName, "original prefab references");
        var config = new TestSettings(); int saves = 0;
        var fpsObj = UnityEngine.Object.Instantiate(asset); var fps = fpsObj.GetComponent<SliderSetting>();
        try {
            int fpsSaves = 0; float savedFps = -1;
            fps.SetSetting((name, value, target) => { fpsSaves++; savedFps = (float)value; }, "fps_limit", config.fps_limit, null, config);
            Require(fps.slider.minValue == 30 && fps.slider.maxValue == 300 && fps.slider.wholeNumbers, "FPS range initialization");
            Require(fps.slider.value == 147f && fps.valueText.text == "147", "FPS displayed value must match slider before any refresh, value=" + fps.slider.value + " text=" + fps.valueText.text);
            Require(fpsSaves == 0, "initializing the control must not save a changed value");
            fps.ControllerInputDir(1, 1f);
            Require(fpsSaves > 0 && savedFps == 148f && fps.slider.value == 148f, "first controller step starts from saved FPS");
        } finally { UnityEngine.Object.DestroyImmediate(fpsObj); }
        Action<string,object,CFSettings> save = (name, value, target) => { Require(name == "fov" && target == config, "callback identity"); config.fov = (float)value; saves++; };
        try
        {
            slider.SetSetting(save, "fov", config.fov, null, config); slider.UpdateValue();
            Require(slider.slider.minValue == 60 && slider.slider.maxValue == 140 && slider.slider.wholeNumbers && slider.slider.value == 85, "prefab initialization and value refresh");
            slider.slider.SetValueWithoutNotify(100); slider.UpdateValueSlider(); Require(config.fov == 100 && saves > 0, "slider change saves field");
            slider.valueText.SetTextWithoutNotify("200"); slider.UpdateValueInputField(); Require(config.fov == 140 && slider.slider.value == 140, "typed input clamps to maximum");
            slider.ControllerInputDir(-1, 1f); Require(config.fov == 139, "controller uses one-unit step");
            Debug.Log("SettingsSliderPortCheck passed: original prefab, slider ranges, edits and callback");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
}
