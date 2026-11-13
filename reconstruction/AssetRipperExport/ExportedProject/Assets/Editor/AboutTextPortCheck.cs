using System;
using Assets.Scripts.Settings___Saves;
using TMPro;
using UnityEngine;

public static class AboutTextPortCheck
{
    public static void Run()
    {
        var go = new GameObject("AboutText check", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(AboutText));
        try
        {
            var label = go.GetComponent<TextMeshProUGUI>();
            var about = go.GetComponent<AboutText>();
            about.text = label;
            about.SetText("Heading", "Body");
            if (label.text != "Heading<size=75%>\n\nBody") throw new Exception("Header/body formatting mismatch");
            about.SetText(null, null);
            if (label.text != "<size=75%>\n\n") throw new Exception("Null concatenation mismatch");
            Debug.Log("AboutTextPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
