#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using TMPro;
using UnityEngine;

// Opt-in raster check of the font material actually loaded by the menu.
internal sealed class AndroidUnderlineRenderProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_underline_render_probe", false)) return;
        var root = new GameObject("underline render probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidUnderlineRenderProbe>();
    }

    IEnumerator Start()
    {
        TMP_Text original = null; float deadline = Time.realtimeSinceStartup + 90;
        while (Time.realtimeSinceStartup < deadline && original == null)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                if (t != null && t.font != null && t.font.name == "alagard SDF" && t.fontSharedMaterial != null) { original = t; break; }
            if (original == null) yield return null;
        }
        if (original == null) { Debug.LogError("Underline render probe INCONCLUSIVE: menu font unavailable"); yield break; }
        var root = new GameObject("underline render scene"); root.transform.position = new Vector3(0,10000,0); root.layer = 30;
        var cameraObject = new GameObject("camera"); cameraObject.transform.SetParent(root.transform, false); cameraObject.transform.localPosition = new Vector3(0,0,-100);
        var cam = cameraObject.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 20; cam.aspect = 2; cam.cullingMask = 1 << 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.allowHDR = false; cam.allowMSAA = false;
        var textObject = new GameObject("text"); textObject.layer = 30; textObject.transform.SetParent(root.transform, false);
        var text = textObject.AddComponent<TextMeshPro>(); text.font = original.font; text.fontSharedMaterial = original.fontSharedMaterial; text.fontSize = 42; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(220,100); text.color = Color.white;
        var rt = new RenderTexture(512,256,24,RenderTextureFormat.ARGB32); var image = new Texture2D(512,256,TextureFormat.RGBA32,false); var previous = RenderTexture.active; cam.targetTexture = rt;
        int Measure(string value, bool bold, string file)
        {
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; text.text = value; text.ForceMeshUpdate(true,true);
            cam.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,512,256),0,0); image.Apply();
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" + file + ".png", image.EncodeToPNG());
            float minX = 512, maxX = 0, minY = 256;
            foreach (var c in text.textInfo.characterInfo) if (c.isVisible) {
                var lo = cam.WorldToScreenPoint(text.transform.TransformPoint(c.bottomLeft)); var hi = cam.WorldToScreenPoint(text.transform.TransformPoint(c.topRight));
                minX = Mathf.Min(minX,lo.x); maxX = Mathf.Max(maxX,hi.x); minY = Mathf.Min(minY,lo.y);
            }
            int longest = 0, left = Mathf.Clamp(Mathf.FloorToInt(minX),0,511), right = Mathf.Clamp(Mathf.CeilToInt(maxX),0,511);
            for (int y=Mathf.Max(0,Mathf.FloorToInt(minY)-20); y<Mathf.Min(256,Mathf.FloorToInt(minY)); y++) {
                int run=0; for (int x=left;x<=right;x++) { var p=image.GetPixel(x,y); if (p.r>.3f || p.g>.3f || p.b>.3f) { run++; longest=Mathf.Max(longest,run); } else run=0; }
            }
            Debug.Log($"Underline render probe {file}: longestBelowGlyph={longest} bounds={left},{right},{minY:F1} shader={text.fontSharedMaterial.shader.name}");
            return longest;
        }
        foreach (var shaderName in new[] { "TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field", "TextMeshPro/Distance Field Overlay" })
        {
            var shader = Shader.Find(shaderName);
            if (shader == null || !shader.isSupported) { Debug.LogError("Underline render probe INCONCLUSIVE: shader unavailable " + shaderName); continue; }
            var variant = new Material(original.fontSharedMaterial); variant.shader = shader; text.fontSharedMaterial = variant;
            string suffix = shaderName.Contains("Mobile") ? "mobile" : shaderName.Contains("Overlay") ? "overlay" : "standard";
            int normal=Measure("PLAY",false,"underline-"+suffix+"-normal");
            int bold=Measure("PLAY",true,"underline-"+suffix+"-bold");
            int intentional=Measure("<u>PLAY</u>",false,"underline-"+suffix+"-intentional");
            bool pass = intentional > 30 && normal < intentional / 2 && bold < intentional / 2;
            Debug.Log($"Underline render probe {(pass ? "PASS" : "INCONCLUSIVE")} {suffix}: normal={normal} bold={bold} intentional={intentional}");
            Destroy(variant);
        }
        RenderTexture.active=previous; cam.targetTexture=null; rt.Release(); Destroy(rt); Destroy(image); Destroy(root); Destroy(gameObject);
    }
}
#endif
