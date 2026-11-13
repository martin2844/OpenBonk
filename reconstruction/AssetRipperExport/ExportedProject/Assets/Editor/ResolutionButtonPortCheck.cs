using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class ResolutionButtonPortCheck
{
    public static void Run()
    {
        var obj = new GameObject("resolution button test", typeof(RectTransform)); obj.SetActive(false);
        var image = obj.AddComponent<Image>(); var uiButton = obj.AddComponent<Button>(); var button = obj.AddComponent<MyButtonSettingRes>();
        button.background = image; button.defaultColor = Color.red; button.hoverColor = Color.green;
        int clicks = 0; Action listener = () => clicks++; MyButtonSettingRes.A_Clicked += listener;
        try
        {
            obj.SetActive(true);
            typeof(MyButton).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, null);
            button.StartHover();
            var hover = typeof(MyButton).GetField("isHovering", BindingFlags.Instance | BindingFlags.NonPublic);
            if (image.color != Color.green || !(bool)hover.GetValue(button)) throw new Exception("hover state");
            button.StopHover();
            if (image.color != Color.red || (bool)hover.GetValue(button)) throw new Exception("unhover state");
            uiButton.onClick.Invoke();
            if (clicks != 1) throw new Exception($"expected one click event, got {clicks}");
            Debug.Log("ResolutionButtonPortCheck passed: hover colors, state and click event");
        }
        finally { MyButtonSettingRes.A_Clicked -= listener; UnityEngine.Object.DestroyImmediate(obj); }
    }
}
