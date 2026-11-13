#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Opt-in rendering diagnostic; clones loaded game materials and isolates its camera.
internal sealed class AndroidEffectsAlphaProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_effects_alpha_probe", false)) return;
        var root = new GameObject("effects alpha probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidEffectsAlphaProbe>();
    }
    IEnumerator Start()
    {
        Material beam = null, dissolve = null; float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (beam == null || dissolve == null))
        {
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>()) {
                if (material.name == "BeamLightning") beam = material;
                if (material.name == "Skull") dissolve = material;
            }
            if (beam == null || dissolve == null) yield return new WaitForSecondsRealtime(1f);
        }
        if (beam == null || dissolve == null) { Debug.LogError($"Effects alpha probe INCONCLUSIVE: beam={beam}, dissolve={dissolve}"); yield break; }
        var a = new Texture2D(1,1,TextureFormat.RGBA32,false,true); var b = new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.layer = 30; quad.transform.position = new Vector3(0,10000,0); quad.transform.localScale = Vector3.one * 2;
        var mesh = Instantiate(quad.GetComponent<MeshFilter>().sharedMesh); quad.GetComponent<MeshFilter>().sharedMesh = mesh; mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        var camObject = new GameObject("effects alpha camera"); camObject.transform.position = new Vector3(0,10000,-4); var cam = camObject.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 1; cam.aspect = 1; cam.cullingMask = 1 << 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.blue; cam.allowHDR = false; cam.allowMSAA = false;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image = new Texture2D(64,64,TextureFormat.RGBA32,false); cam.targetTexture = target; var oldTarget = RenderTexture.active;
        var matBeam = new Material(beam); var matDissolve = new Material(dissolve);
        Color Read() { cam.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
        void Pixel(Texture2D t, Color color) { t.SetPixel(0,0,color); t.Apply(); }
        matBeam.SetTexture("_TextureSample",a); matBeam.SetTexture("_TextureSample2",b); matBeam.SetVector("_ScrollSpeed",Vector4.zero); matBeam.SetVector("_ScrollSpeed2",Vector4.zero); matBeam.SetColor("_Tint",Color.white); matBeam.SetFloat("_ExtraGlow",1); quad.GetComponent<Renderer>().sharedMaterial = matBeam;
        Pixel(a,new Color(1,0,0,0)); Pixel(b,Color.white); Color beamClear1 = Read(); Pixel(a,Color.red); Pixel(b,new Color(1,1,1,0)); Color beamClear2 = Read(); Pixel(b,Color.white); Color beamVisible = Read();
        bool beamOk = beam.shader.isSupported && beamClear1.b > .9f && beamClear1.r < .05f && beamClear2.b > .9f && beamClear2.r < .05f && beamVisible.r > .8f;
        matDissolve.SetTexture("_TextureSample",a); matDissolve.SetTexture("_Ramp",b); matDissolve.SetColor("_Tint",Color.white); matDissolve.SetFloat("_ExtraGlow",1); quad.GetComponent<Renderer>().sharedMaterial = matDissolve;
        Pixel(a,new Color(1,0,0,0)); Pixel(b,Color.white); Color dissolveClear = Read(); Pixel(a,Color.red); Pixel(b,new Color(.5f,.5f,.5f,1)); Color dissolveVisible = Read();
        bool dissolveOk = dissolve.shader.isSupported && dissolveClear.b > .9f && dissolveClear.r < .05f && dissolveVisible.r > .8f;
        Debug.Log($"Effects alpha probe {(beamOk && dissolveOk ? "PASS" : "FAIL")}: beam={beamOk} clear1={beamClear1} clear2={beamClear2} visible={beamVisible}; dissolve={dissolveOk} clear={dissolveClear} visible={dissolveVisible}");
        int AuthoredPixels(Material source, string first, string second)
        {
            if (source.GetTexture(first) == null || source.GetTexture(second) == null) return -1;
            quad.GetComponent<Renderer>().sharedMaterial = source;
            mesh.colors = new[] { Color.clear, Color.clear, Color.clear, Color.clear }; Read(); Color32[] transparent = image.GetPixels32();
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white }; Read(); Color32[] visible = image.GetPixels32();
            int count = 0; for (int i = 0; i < visible.Length; i++) if (Mathf.Abs(visible[i].r - transparent[i].r) > 8 || Mathf.Abs(visible[i].g - transparent[i].g) > 8 || Mathf.Abs(visible[i].b - transparent[i].b) > 8) count++;
            return count;
        }
        int beamPixels = AuthoredPixels(beam, "_TextureSample", "_TextureSample2");
        int dissolvePixels = AuthoredPixels(dissolve, "_TextureSample", "_Ramp");
        Debug.Log($"Effects authored texture probe {(beamPixels > 0 && dissolvePixels > 0 ? "PASS" : "FAIL")}: beamPixels={beamPixels} dissolvePixels={dissolvePixels}");
        RenderTexture.active = oldTarget; cam.targetTexture = null; target.Release(); Destroy(target); Destroy(image); Destroy(camObject); Destroy(quad); Destroy(mesh); Destroy(matBeam); Destroy(matDissolve); Destroy(a); Destroy(b); Destroy(gameObject);
    }
}
#endif
