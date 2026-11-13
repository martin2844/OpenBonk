using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AndroidControlLabelCheck
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ControlSetting.prefab");
        var row = UnityEngine.Object.Instantiate(prefab); row.SetActive(false);
        try
        {
            var display = row.GetComponentInChildren<KeyDisplay>(true);
            if (display == null || display.text == null || display.background == null || display.textSizer == null) throw new Exception("Original key display references missing");
            var apply = typeof(InputSetting).GetMethod("SetNativeLabel", BindingFlags.Static | BindingFlags.NonPublic);
            apply.Invoke(null, new object[] { display, "Press button" });
            if (display.background.enabled || !display.text.gameObject.activeSelf || display.text.color != Color.white || display.text.text != "Press button") throw new Exception("Listening label is not legible or is hidden");
            apply.Invoke(null, new object[] { display, "A / RB" });
            if (display.background.enabled || display.text.text != "A / RB" || !display.text.gameObject.activeSelf) throw new Exception("Binding label is not restored");
            if (display.glyph != null && display.glyph.gameObject.activeSelf) throw new Exception("Old glyph remains visible");
            Debug.Log("AndroidControlLabelCheck PASS");
        }
        finally { UnityEngine.Object.DestroyImmediate(row); }
    }
}
