using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HoverHighlightPortCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        var component = UnityEngine.Object.FindObjectOfType<HoverHighlight>(true);
        if (component == null) throw new Exception("Scene highlight missing");
        var image = component.GetComponent<MaskableGraphic>();
        if (image == null || component.gameObject.name != "Info") throw new Exception("Scene graphic mismatch");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(HoverHighlight).GetMethod("Awake", flags).Invoke(component, null);
        typeof(HoverHighlight).GetMethod("OnPointerEnter", flags).Invoke(component, null);
        if (image.color != component.highlightColor || !(bool)typeof(HoverHighlight).GetField("mouseOver", flags).GetValue(component)) throw new Exception("Enter failed");
        typeof(HoverHighlight).GetMethod("OnPointerExit", flags).Invoke(component, null);
        if (image.color != component.defaultColor || (bool)typeof(HoverHighlight).GetField("mouseOver", flags).GetValue(component)) throw new Exception("Exit failed");
        Debug.Log("HoverHighlightPortCheck passed: real MainMenu Info graphic enter/exit");
    }
}
