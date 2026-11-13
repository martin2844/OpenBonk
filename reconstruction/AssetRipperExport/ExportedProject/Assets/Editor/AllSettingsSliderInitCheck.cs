using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEngine;

public static class AllSettingsSliderInitCheck
{
    public static void Run()
    {
        var config = new ConfigSaveFile();
        CFSettings[] categories = { config.cfGameSettings, config.cfVideoSettings, config.cfAudioSettings, config.cfControlSettings, config.cfVisualsSettings, config.cfOtherSettings };
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SliderSetting.prefab");
        if (prefab == null) throw new Exception("Missing slider prefab");
        int cases=0, failures=0;
        foreach (var category in categories) foreach (var field in category.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance)) {
            if (field.FieldType != typeof(float)) continue;
            ConfigSettingsUtility.GetSliderRange(field.Name,out float min,out float max);
            bool whole = ConfigSettingsUtility.GetSliderWholeNumbers(field.Name);
            foreach (float raw in new[] { min, min+(max-min)*.37f, max, (float)field.GetValue(category) }) {
                float value = whole ? Mathf.Round(raw) : raw;
                var obj = UnityEngine.Object.Instantiate(prefab); var setting = obj.GetComponent<SliderSetting>(); int callbacks=0;
                try {
                    setting.SetSetting((name, v, target) => callbacks++, field.Name, value, null, category);
                    bool ok = Mathf.Abs(setting.slider.value-value)<.0005f && callbacks==0;
                    if (!ok) { failures++; Debug.Log($"Slider init FAIL {field.Name} expected={value} actual={setting.slider.value} callbacks={callbacks} range={min}..{max}"); }
                    cases++;
                } catch (Exception e) { failures++; Debug.Log($"Slider init FAIL {field.Name}: {e}"); }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
        }
        Debug.Log($"AllSettingsSliderInitCheck RESULT cases={cases} failures={failures}");
        if (failures != 0) throw new Exception("Slider initialization audit failed");
    }
}
