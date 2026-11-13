#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Render isolated copies of real menu materials only when explicitly enabled.
internal sealed class AndroidRainbowSilhouetteProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_rainbow_silhouette_probe", false)) return;
        var go = new GameObject("rainbow silhouette probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidRainbowSilhouetteProbe>();
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(8f);
        Material whiteSource = null, rainbowSource = null;
        foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (m.name == "WhiteUiOverlay") whiteSource = m;
            if (m.name == "rainbow") rainbowSource = m;
        }
        if (whiteSource == null || rainbowSource == null || !whiteSource.shader.isSupported || !rainbowSource.shader.isSupported) { Debug.LogError($"Rainbow silhouette probe FAIL: materials or shaders unavailable: white={whiteSource}, rainbow={rainbowSource}"); yield break; }
        var white = new Material(whiteSource); var rainbow = new Material(rainbowSource);
        var texture = new Texture2D(1,1,TextureFormat.RGBA32,false,true); white.mainTexture = texture; rainbow.SetFloat("_Speed", 0);
        const int layer = 30;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "isolated shader quad"; quad.layer = layer; quad.transform.position = new Vector3(0,10000,0); quad.transform.localScale = Vector3.one*2; var renderer = quad.GetComponent<Renderer>(); renderer.sharedMaterial = white;
        var camGo = new GameObject("isolated shader camera"); camGo.transform.position = new Vector3(0,10000,-4); var cam = camGo.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 1; cam.aspect = 1; cam.cullingMask = 1<<layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.blue; cam.allowHDR = false; cam.allowMSAA = false;
        var target = new RenderTexture(120,60,24,RenderTextureFormat.ARGB32); var image = new Texture2D(120,60,TextureFormat.RGBA32,false); cam.targetTexture = target; var oldActive = RenderTexture.active; bool oldFog = RenderSettings.fog; RenderSettings.fog = false;
        void Render() { cam.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,120,60),0,0); image.Apply(); }
        texture.SetPixel(0,0,new Color(1,0,0,0)); texture.Apply(); Render(); var clear = image.GetPixel(60,30);
        texture.SetPixel(0,0,Color.red); texture.Apply(); Render(); var opaque = image.GetPixel(60,30);
        renderer.sharedMaterial = rainbow; Render(); var colors = new HashSet<Color32>(); for(int x=10;x<120;x+=20) colors.Add(image.GetPixel(x,30));
        bool ok = clear.b > .9f && clear.r < .05f && opaque.r > .9f && opaque.g > .9f && opaque.b > .9f && colors.Count == 6;
        Debug.Log($"Rainbow silhouette probe {(ok ? "PASS" : "FAIL")}: transparent={clear}, opaque={opaque}, rainbowBands={colors.Count}");
        RenderSettings.fog = oldFog; RenderTexture.active = oldActive; cam.targetTexture = null; target.Release(); Destroy(target); Destroy(image); Destroy(camGo); Destroy(quad); Destroy(white); Destroy(rainbow); Destroy(texture);
    }
}
#endif
