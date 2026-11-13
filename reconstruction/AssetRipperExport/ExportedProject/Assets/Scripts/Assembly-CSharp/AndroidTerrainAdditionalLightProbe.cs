#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Opt-in isolated rendering check; does not alter normal gameplay.
internal sealed class AndroidTerrainAdditionalLightProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_terrain_add_light_probe", false)) return;
        var root = new GameObject("Terrain additional light probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidTerrainAdditionalLightProbe>();
    }
    static Texture2D Make(Color c) { var t=new Texture2D(1,1,TextureFormat.RGBA32,false); t.SetPixel(0,0,c); t.Apply(); return t; }
    static Color Read(Camera cam, RenderTexture target, Texture2D pixel) { cam.Render(); RenderTexture.active=target; pixel.ReadPixels(new Rect(32,32,1,1),0,0); pixel.Apply(); return pixel.GetPixel(0,0); }
    IEnumerator Start()
    {
        yield return null;
        var shader=Shader.Find("Toon/Lit Tri Planar Normal");
        if (!shader || !shader.isSupported) { Debug.LogError("Terrain additional light probe INCONCLUSIVE: shader unavailable"); yield break; }
        var material=new Material(shader);
        foreach (var name in new[]{"_MainTex","_MainTexSide","_BelowYTexture","_Noise","_Ramp"}) material.SetTexture(name,Make(Color.white));
        foreach (var name in new[]{"_NormalT","_Normal"}) material.SetTexture(name,Make(new Color(.5f,.5f,1,1)));
        material.SetColor("_Color",Color.white); material.SetColor("_EdgeColor",Color.black); material.SetFloat("_TopSpread",.5f); material.SetFloat("_EdgeWidth",.4f); material.SetFloat("_BelowYThreshold",-100f); material.SetFloat("_BelowYBlend",2); material.SetFloat("_OutlineWidth",0);
        var root=new GameObject("isolated lighting scene"); root.transform.position=new Vector3(0,10000,0);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.SetParent(root.transform,false); quad.layer=30; quad.GetComponent<Renderer>().sharedMaterial=material;
        var dirObject=new GameObject("main"); dirObject.transform.SetParent(root.transform,false); var dir=dirObject.AddComponent<Light>(); dir.type=LightType.Directional; dir.intensity=.5f; dir.color=Color.red; dir.cullingMask=1<<30; dir.renderMode=LightRenderMode.ForcePixel;
        var pointObject=new GameObject("additional"); pointObject.transform.SetParent(root.transform,false); pointObject.transform.localPosition=new Vector3(0,0,-2); var point=pointObject.AddComponent<Light>(); point.type=LightType.Point; point.intensity=1; point.range=10; point.color=Color.green; point.cullingMask=1<<30; point.renderMode=LightRenderMode.ForcePixel;
        var cameraObject=new GameObject("camera"); cameraObject.transform.SetParent(root.transform,false); cameraObject.transform.localPosition=new Vector3(0,0,-4); var cam=cameraObject.AddComponent<Camera>(); cam.renderingPath=RenderingPath.Forward; cam.orthographic=true; cam.orthographicSize=1; cam.cullingMask=1<<30; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Color.black;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); cam.targetTexture=target; var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false); var prev=RenderTexture.active;
        int oldLights=QualitySettings.pixelLightCount; bool oldFog=RenderSettings.fog; QualitySettings.pixelLightCount=2; RenderSettings.fog=false;
        yield return null;
        point.enabled=false; var baseline=Read(cam,target,pixel); point.enabled=true; var lit=Read(cam,target,pixel);
        bool ok=baseline.r>.3f && lit.g>baseline.g+.05f && Mathf.Abs(lit.r-baseline.r)<.05f;
        Debug.Log($"Terrain additional light probe {(ok ? "PASS" : "FAIL")}: baseline={baseline} point={lit} graphics={SystemInfo.graphicsDeviceType}");
        QualitySettings.pixelLightCount=oldLights; RenderSettings.fog=oldFog; RenderTexture.active=prev; cam.targetTexture=null; target.Release(); Destroy(pixel); Destroy(target); Destroy(material); Destroy(root);
    }
}
#endif
