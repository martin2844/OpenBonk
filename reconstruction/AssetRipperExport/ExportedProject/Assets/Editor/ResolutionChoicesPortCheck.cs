using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Settings___Saves.SaveFiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ResolutionChoicesPortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception("ResolutionChoicesPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("resolution choices", typeof(RectTransform)); root.SetActive(false);
        var window = root.AddComponent<ResolutionWindow>();
        var template = new GameObject("template", typeof(RectTransform)); template.transform.SetParent(root.transform, false);
        template.AddComponent<Image>(); template.AddComponent<Button>();
        var button = template.AddComponent<MyButtonResolution>();
        button.t_resolution = new GameObject("label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); button.t_resolution.transform.SetParent(template.transform, false);
        button.selected = new GameObject("selected"); button.selected.transform.SetParent(template.transform, false);
        window.resolutionButtonPrefab = template;
        var refresh = typeof(ResolutionWindow).GetMethod("RefreshChoices", BindingFlags.Instance | BindingFlags.NonPublic);
        var listField = typeof(ResolutionWindow).GetField("resolutionButtons", BindingFlags.Instance | BindingFlags.NonPublic);
        var resolutions = new[] { new Resolution { width = 1280, height = 720 }, new Resolution { width = 1920, height = 1080 } };
        var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile();
        CurrentSettings.Instance = root.AddComponent<CurrentSettings>();
        try
        {
            Check(ConfigSettingsUtility.GetCurrentResolutionIndex(resolutions, -1, 1920, 1080) == 1, "unset config resolves current screen size");
            Check(ConfigSettingsUtility.GetCurrentResolutionIndex(resolutions, 0, 1920, 1080) == 0, "valid configured choice remains selected");
            Check(ConfigSettingsUtility.GetCurrentResolutionIndex(resolutions, 99, 1600, 900) == -1, "unavailable screen size has no matching choice");
            refresh.Invoke(window, new object[] { resolutions, 1 });
            var choices = (List<MyButtonResolution>)listField.GetValue(window);
            Check(choices.Count == 2 && choices[0].t_resolution.text == "1280x720" && choices[1].t_resolution.text == "1920x1080", "labels and count");
            Check(!choices[0].selected.activeSelf && choices[1].selected.activeSelf, "saved selection");
            choices[0].ClickButton(); Check(save.config.cfVideoSettings.resolution == 0, "click updates configured index");
            var changed = typeof(MyButtonResolution).GetMethod("OnResolutionChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            changed.Invoke(choices[0], new object[] { 0 }); changed.Invoke(choices[1], new object[] { 0 });
            Check(choices[0].selected.activeSelf && !choices[1].selected.activeSelf, "selection indicator follows change event");
            refresh.Invoke(window, new object[] { new[] { resolutions[0] }, 0 });
            Check(choices.Count == 2 && choices[0].gameObject.activeSelf && !choices[1].gameObject.activeSelf, "reuse and hide excess choices");
            Debug.Log("ResolutionChoicesPortCheck passed: labels, selection, saved index and pooled choices");
        }
        finally { CurrentSettings.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
