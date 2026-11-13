#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Opt-in inspection of the real gameplay camera and its post-process resources.
internal sealed class AndroidPostProcessingProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_postprocess_probe", false)) return;
        var go = new GameObject("Post-processing probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPostProcessingProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null)) yield return null;
        if (PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null) { Debug.LogError("Post-processing probe: gameplay camera timeout"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        Camera cam=PlayerCamera.Instance.camera;
        var layer=cam.GetComponent<PostProcessLayer>();
        var field=typeof(PostProcessLayer).GetField("m_Resources",BindingFlags.Instance|BindingFlags.NonPublic);
        var resources=layer && field != null ? field.GetValue(layer) as PostProcessResources : null;
        var shader=resources != null && resources.shaders != null ? resources.shaders.bloom : null;
        bool pass=layer && layer.isActiveAndEnabled && resources && shader && shader.isSupported;
        Debug.Log($"Post-processing probe {(pass ? "PASS" : "FAIL")}: camera={cam.name}, layer={(layer != null)}, enabled={(layer != null && layer.isActiveAndEnabled)}, resources={(resources != null)}, bloom={(shader != null ? shader.name : "missing")}, supported={(shader != null && shader.isSupported)}");
        if (!pass) yield break;
        yield return new WaitForEndOfFrame();
        var go = new GameObject("Temporary controlled grading volume");
        int mask = layer.volumeLayer.value, volumeLayer = -1;
        for (int i=0;i<32;i++) if ((mask & (1<<i)) != 0) { volumeLayer=i;break; }
        if (volumeLayer < 0) { Debug.LogError("Post-processing pixels FAIL: empty volume mask"); Destroy(go); yield break; }
        go.layer=volumeLayer;
        var volume=go.AddComponent<PostProcessVolume>();volume.isGlobal=true;volume.priority=10000f;volume.weight=1f;
        var profile=ScriptableObject.CreateInstance<PostProcessProfile>();volume.sharedProfile=profile;
        var grading=profile.AddSettings<ColorGrading>();grading.enabled.Override(true);grading.postExposure.Override(2f);
        var target=new RenderTexture(256,144,24,RenderTextureFormat.ARGB32);var image=new Texture2D(256,144,TextureFormat.RGBA32,false);
        var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
        try {
            cam.targetTexture=target;layer.enabled=false;cam.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,144),0,0);image.Apply();var before=image.GetPixels32();
            layer.enabled=true;cam.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,144),0,0);image.Apply();var after=image.GetPixels32();
            int changed=0;long sum=0;for(int i=0;i<before.Length;i++){int d=System.Math.Abs(before[i].r-after[i].r)+System.Math.Abs(before[i].g-after[i].g)+System.Math.Abs(before[i].b-after[i].b);if(d>12)changed++;sum+=d;}
            Debug.Log($"Post-processing pixels {(changed>500 ? "PASS" : "FAIL")}: same camera, forced exposure 2, changed={changed}/{before.Length}, meanRgbDelta={(float)sum/before.Length:F2}");
        } catch (System.Exception e) { Debug.LogError("Post-processing pixels FAIL: "+e); }
        finally { cam.targetTexture=oldTarget;RenderTexture.active=oldActive;layer.enabled=true;Destroy(go);Destroy(profile);target.Release();Destroy(target);Destroy(image); }

        // Render an isolated HDR patch through the real camera and post-process
        // resources, checking for bloom outside the patch rather than inside it.
        var shaderTest = Shader.Find("Custom/UnlitTransparentBlended");
        if (shaderTest == null || !shaderTest.isSupported) { Debug.LogError("Post-processing bloom INCONCLUSIVE: test shader unavailable"); yield break; }
        var bloomObject = new GameObject("Temporary controlled bloom volume"); bloomObject.layer = volumeLayer;
        var bloomVolume = bloomObject.AddComponent<PostProcessVolume>(); bloomVolume.isGlobal = true; bloomVolume.priority = 10000f; bloomVolume.weight = 1f;
        var bloomProfile = ScriptableObject.CreateInstance<PostProcessProfile>(); bloomVolume.sharedProfile = bloomProfile;
        var bloom = bloomProfile.AddSettings<UnityEngine.Rendering.PostProcessing.Bloom>(); bloom.enabled.Override(true); bloom.intensity.Override(8f); bloom.threshold.Override(.8f); bloom.softKnee.Override(.5f);
        var patch = GameObject.CreatePrimitive(PrimitiveType.Quad); patch.layer = 30;
        var patchMaterial = new Material(shaderTest); patchMaterial.SetTexture("_MainTex", Texture2D.whiteTexture); patchMaterial.SetColor("_Color", new Color(8f,8f,8f,1f)); patch.GetComponent<Renderer>().sharedMaterial = patchMaterial;
        patch.transform.position = cam.transform.position + cam.transform.forward * 3f; patch.transform.rotation = cam.transform.rotation; patch.transform.localScale = Vector3.one * .4f;
        var bloomTarget = new RenderTexture(256,144,24,RenderTextureFormat.ARGBHalf); var bloomImage = new Texture2D(256,144,TextureFormat.RGBA32,false);
        int oldMask = cam.cullingMask; bool oldHdr = cam.allowHDR; var oldClear = cam.clearFlags; Color oldBackground = cam.backgroundColor; oldTarget = cam.targetTexture; oldActive = RenderTexture.active;
        try {
            cam.cullingMask = 1 << 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.allowHDR = true; cam.targetTexture = bloomTarget;
            Color32[] Read(bool enabled) { layer.enabled = enabled; cam.Render(); RenderTexture.active = bloomTarget; bloomImage.ReadPixels(new Rect(0,0,256,144),0,0); bloomImage.Apply(); return bloomImage.GetPixels32(); }
            Color32[] off = Read(false), on = Read(true); int changed = 0, baselineDark = 0, maxIncrease = 0, minX=256, maxX=-1, minY=144, maxY=-1;
            for (int y=0;y<144;y++) for (int x=0;x<256;x++) { int i=y*256+x; if (off[i].r+off[i].g+off[i].b>600) { minX=Mathf.Min(minX,x); maxX=Mathf.Max(maxX,x); minY=Mathf.Min(minY,y); maxY=Mathf.Max(maxY,y); } }
            for (int y=0;y<144;y++) for (int x=0;x<256;x++) {
                int i=y*256+x; if (x>=minX && x<=maxX && y>=minY && y<=maxY) continue;
                int before=off[i].r+off[i].g+off[i].b, after=on[i].r+on[i].g+on[i].b;
                if (before<6) { baselineDark++; maxIncrease = Mathf.Max(maxIncrease, after-before); if (after-before>6) changed++; }
            }
            int mid=72*256+128;
            Debug.Log($"Post-processing bloom {(off[mid].r < 200 ? "INCONCLUSIVE" : changed > 0 ? "PASS" : "FAIL")}: changedOutsidePatch={changed}, darkBaselinePixels={baselineDark}, maxIncrease={maxIncrease}, bounds={minX},{minY}..{maxX},{maxY}, centerOff={off[mid]}, centerOn={on[mid]}, cameraHDR={cam.allowHDR}, volumeMask={mask}, shader={shaderTest.name}");
        } catch (System.Exception e) { Debug.LogError("Post-processing bloom FAIL: " + e); }
        finally { cam.cullingMask=oldMask; cam.clearFlags=oldClear; cam.backgroundColor=oldBackground; cam.allowHDR=oldHdr; cam.targetTexture=oldTarget; RenderTexture.active=oldActive; layer.enabled=true; Destroy(patch); Destroy(patchMaterial); Destroy(bloomObject); Destroy(bloomProfile); bloomTarget.Release(); Destroy(bloomTarget); Destroy(bloomImage); }
    }
}
#endif
