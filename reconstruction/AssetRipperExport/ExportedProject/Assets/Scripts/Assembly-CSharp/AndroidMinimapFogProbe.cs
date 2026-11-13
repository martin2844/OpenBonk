#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Activated only when explicitly requested by an Android intent.
internal sealed class AndroidMinimapFogProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_minimap_fog_probe", false)) return;
        var go = new GameObject("Minimap fog probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidMinimapFogProbe>();
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(2);
        bool oldFog = RenderSettings.fog; FogMode oldMode = RenderSettings.fogMode;
        Color oldColor = RenderSettings.fogColor; float oldStart = RenderSettings.fogStartDistance, oldEnd = RenderSettings.fogEndDistance, oldDensity = RenderSettings.fogDensity;
        int oldLights = QualitySettings.pixelLightCount;
        var cameraObject = new GameObject("minimap fog probe camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 30; camera.renderingPath = RenderingPath.Forward;
        var target = new RenderTexture(64, 64, 24); camera.targetTexture = target;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.layer = 30;
        quad.transform.position = new Vector3(0, 0, 4); quad.transform.localScale = Vector3.one * 4;
        var shader = Shader.Find("Custom/MinimapHeightColorGradient");
        if (shader == null || !shader.isSupported) { Debug.LogError("Android minimap fog INCONCLUSIVE: shader unavailable"); Destroy(cameraObject); Destroy(quad); Destroy(gameObject); yield break; }
        var material = new Material(shader); material.SetColor("_Color", Color.green); material.SetColor("_MaxColor", Color.green);
        material.SetFloat("_MinDistance", -1); material.SetFloat("_MaxDistance", 1); quad.GetComponent<Renderer>().sharedMaterial = material;
        var sunObject = new GameObject("minimap fog probe sun"); var sun = sunObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.cullingMask = 1 << 30;
        var lampObject = new GameObject("minimap fog probe lamp"); lampObject.transform.position = new Vector3(0, 0, 2);
        var lamp = lampObject.AddComponent<Light>(); lamp.type = LightType.Point; lamp.range = 10; lamp.renderMode = LightRenderMode.ForcePixel; lamp.cullingMask = 1 << 30;
        var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = Color.red;
        RenderSettings.fogDensity = 2; QualitySettings.pixelLightCount = 4;
        yield return null;
        var controlShader = Shader.Find("Unlit/Texture");
        var controlMaterial = controlShader ? new Material(controlShader) : null;
        var controlTexture = new Texture2D(1, 1); controlTexture.SetPixel(0, 0, Color.green); controlTexture.Apply();
        Color control = Color.clear;
        if (controlMaterial != null)
        {
            controlMaterial.mainTexture = controlTexture; quad.GetComponent<Renderer>().sharedMaterial = controlMaterial;
            camera.Render(); RenderTexture.active = target; pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); control = pixel.GetPixel(0, 0);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }
        camera.Render(); RenderTexture.active = target; pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var fogged = pixel.GetPixel(0, 0);
        lamp.enabled = false; sun.enabled = false; RenderSettings.fog = false;
        yield return null;
        camera.Render(); RenderTexture.active = target;
        pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var clear = pixel.GetPixel(0, 0);
        bool controlValid = control.r > .5f && control.g < .3f;
        bool pass = fogged.r > .5f && fogged.g < .3f && clear.g > .5f && clear.r < .3f;
        Debug.Log($"Android minimap fog {(!controlValid ? "INCONCLUSIVE" : pass ? "PASS" : "FAIL")}: control={control}, fogWithPixelLight={fogged}, clear={clear}, graphics={SystemInfo.graphicsDeviceType}");
        RenderSettings.fog = oldFog; RenderSettings.fogMode = oldMode; RenderSettings.fogColor = oldColor;
        RenderSettings.fogStartDistance = oldStart; RenderSettings.fogEndDistance = oldEnd; RenderSettings.fogDensity = oldDensity; QualitySettings.pixelLightCount = oldLights;
        RenderTexture.active = null; Destroy(pixel); if (controlMaterial != null) Destroy(controlMaterial); Destroy(controlTexture); Destroy(material); Destroy(target); Destroy(quad); Destroy(sunObject); Destroy(lampObject); Destroy(cameraObject); Destroy(gameObject);
    }
}
#endif
