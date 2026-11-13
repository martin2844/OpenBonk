#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in offscreen check of the compiled TextGlow shader on Android.
internal sealed class AndroidTextGlowFogProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_textglow_fog_probe", false)) return;
        var go = new GameObject("TextGlow fog probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidTextGlowFogProbe>();
    }
    IEnumerator Start() {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        var shader = Shader.Find("Archanor VFX/Retro Arsenal/TextGlow");
        if (shader == null || !shader.isSupported) { Debug.LogError("TextGlow fog probe INCONCLUSIVE: shader unavailable"); yield break; }
        Material original=null;
        if (PoolManager.Instance != null && PoolManager.Instance.bonkFx != null)
            foreach (var renderer in PoolManager.Instance.bonkFx.GetComponentsInChildren<Renderer>(true))
                foreach (var candidate in renderer.sharedMaterials)
                    if (candidate != null && candidate.shader == shader) original=candidate;
        var mat = new Material(shader); var texture = new Texture2D(1,1,TextureFormat.RGBA32,false,true); var pan = new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        texture.SetPixel(0,0,new Color(.25f,0,0,1)); texture.Apply(); pan.SetPixel(0,0,Color.black); pan.Apply();
        mat.SetTexture("_TextureSample",texture); mat.SetTexture("_PanningTexture",pan); mat.SetVector("_Speed",Vector4.zero);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); quad.layer=31; quad.transform.position=new Vector3(0,10000,0); quad.transform.localScale=Vector3.one*2f; quad.GetComponent<Renderer>().sharedMaterial=mat;
        var cameraObject=new GameObject("TextGlow fog camera"); var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.transform.position=quad.transform.position+Vector3.back*4f;
        camera.fieldOfView=60; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.blue; camera.cullingMask=1<<31; camera.allowHDR=false; camera.allowMSAA=false;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image=new Texture2D(1,1,TextureFormat.RGBA32,false); camera.targetTexture=target;
        bool oldFog=RenderSettings.fog; var oldMode=RenderSettings.fogMode; var oldColor=RenderSettings.fogColor; float oldDensity=RenderSettings.fogDensity;
        bool linear=Shader.IsKeywordEnabled("FOG_LINEAR"), exp=Shader.IsKeywordEnabled("FOG_EXP"), exp2=Shader.IsKeywordEnabled("FOG_EXP2");
        try {
            Color Render() { camera.Render(); var old=RenderTexture.active; RenderTexture.active=target; image.ReadPixels(new Rect(32,32,1,1),0,0); image.Apply(); RenderTexture.active=old; return image.GetPixel(0,0); }
            RenderSettings.fog=false; Shader.DisableKeyword("FOG_LINEAR"); Shader.DisableKeyword("FOG_EXP"); Shader.DisableKeyword("FOG_EXP2"); var clear=Render();
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=4f; RenderSettings.fogColor=Color.green; Shader.EnableKeyword("FOG_EXP2"); var fogged=Render();
            bool pass=clear.r>.2f && clear.g<.15f && fogged.g>.8f && fogged.r<.15f;
            Debug.Log($"TextGlow fog probe {(pass ? "PASS" : "FAIL")}: shader={shader.name}, clear={clear}, fogged={fogged}, mode=exp2");
            if (original == null) Debug.LogError("TextGlow material probe INCONCLUSIVE: Bonk material unavailable");
            else {
                var real = new Material(original); var full = new Texture2D(64,64,TextureFormat.RGBA32,false);
                try {
                    quad.GetComponent<Renderer>().sharedMaterial=real;
                    Color32[] Capture() { camera.Render(); var old=RenderTexture.active; RenderTexture.active=target; full.ReadPixels(new Rect(0,0,64,64),0,0); full.Apply(); RenderTexture.active=old; return full.GetPixels32(); }
                    RenderSettings.fog=false; Shader.DisableKeyword("FOG_EXP2"); var realClear=Capture();
                    RenderSettings.fog=true; Shader.EnableKeyword("FOG_EXP2"); var realFog=Capture();
                    int changed=0, occupied=0;
                    for(int i=0;i<realClear.Length;i++) {
                        var a=realClear[i]; var b=realFog[i];
                        if (a.r>10 || a.g>10 || a.b<245) occupied++;
                        if (System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b)>12) changed++;
                    }
                    Debug.Log($"TextGlow material probe {(occupied>30 && changed>30 ? "PASS" : "FAIL")}: material={original.name}, texture={original.GetTexture("_TextureSample").name}, panning={original.GetTexture("_PanningTexture").name}, visible={occupied}, fogChanged={changed}");
                } finally { Destroy(real); Destroy(full); }
            }
        } finally {
            if(linear) Shader.EnableKeyword("FOG_LINEAR"); else Shader.DisableKeyword("FOG_LINEAR"); if(exp) Shader.EnableKeyword("FOG_EXP"); else Shader.DisableKeyword("FOG_EXP"); if(exp2) Shader.EnableKeyword("FOG_EXP2"); else Shader.DisableKeyword("FOG_EXP2");
            RenderSettings.fog=oldFog; RenderSettings.fogMode=oldMode; RenderSettings.fogColor=oldColor; RenderSettings.fogDensity=oldDensity;
            camera.targetTexture=null; Destroy(quad); Destroy(cameraObject); Destroy(mat); Destroy(texture); Destroy(pan); Destroy(image); Destroy(target);
        }
    }
}
#endif
