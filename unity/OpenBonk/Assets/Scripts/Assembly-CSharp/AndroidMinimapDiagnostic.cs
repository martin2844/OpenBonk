#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Camera;
using UnityEngine;
using UnityEngine.UI;

// Opt-in only: reads render targets and temporarily isolates the player marker,
// restoring its original layer and material after the controlled render.
internal sealed class AndroidMinimapDiagnostic : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_minimap_probe", false)) return;
        var go = new GameObject("Minimap diagnostic"); DontDestroyOnLoad(go); go.AddComponent<AndroidMinimapDiagnostic>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MyPlayer.Instance == null || GameManager.Instance.cutscene)) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("Minimap probe: game unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        yield return new WaitForEndOfFrame();
        foreach (var controller in FindObjectsByType<MinimapCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var camera = controller.minimapCamera; var icon = controller.playerIcon;
            if (camera == null) { Debug.Log("Minimap probe: camera missing on " + controller.name); continue; }
            var target = camera.targetTexture; var renderer = icon != null ? icon.GetComponent<Renderer>() : null;
            var viewport = icon != null ? camera.WorldToViewportPoint(icon.position) : Vector3.zero;
            Debug.Log($"Minimap probe: controller={controller.name} active={controller.gameObject.activeInHierarchy} camera={camera.name} enabled={camera.enabled} mask={camera.cullingMask} target={(target != null ? target.name : "none")} created={(target != null && target.IsCreated())} format={(target != null ? target.graphicsFormat.ToString() : "none")} icon={(icon != null ? icon.name : "none")} activeIcon={(icon != null && icon.gameObject.activeInHierarchy)} layer={(icon != null ? icon.gameObject.layer : -1)} renderer={(renderer != null && renderer.enabled)} viewport={viewport}");
            if (renderer != null && renderer.sharedMaterial != null)
                Debug.Log($"Minimap probe: icon material={renderer.sharedMaterial.name} shader={(renderer.sharedMaterial.shader != null ? renderer.sharedMaterial.shader.name : "none")} supported={(renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.isSupported)} texture={(renderer.sharedMaterial.mainTexture != null ? renderer.sharedMaterial.mainTexture.name : "none")} color={renderer.sharedMaterial.color} instancing={renderer.sharedMaterial.enableInstancing} forceOff={renderer.forceRenderingOff} bounds={renderer.bounds}");
            if (target == null || !target.IsCreated()) continue;
            foreach (var raw in FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (raw.texture == target) Debug.Log($"Minimap probe: image={raw.name} active={raw.gameObject.activeInHierarchy} enabled={raw.enabled} color={raw.color}");
            var old = RenderTexture.active; Texture2D pixels = null;
            try
            {
                RenderTexture.active = target; pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                var data = pixels.GetPixels32(); int bright=0, blue=0; byte peak=0;
                foreach (var c in data) { int top = Mathf.Max(c.r, Mathf.Max(c.g,c.b)); if (top > 40) bright++; if (c.b > 80 && c.b > c.r * 1.3f && c.b > c.g * 1.1f) blue++; if (top > peak) peak=(byte)top; }
                Debug.Log($"Minimap probe: target pixels={data.Length} bright={bright} blue={blue} peak={peak}");
            }
            finally { RenderTexture.active = old; if (pixels != null) Destroy(pixels); }
            var cameraRoot=new GameObject("Minimap diagnostic camera"); var temp=cameraRoot.AddComponent<Camera>();
            var output=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); Texture2D image=null;
            try
            {
                temp.CopyFrom(camera); temp.enabled=false; temp.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
                temp.targetTexture=output; temp.allowHDR=false; temp.Render();
                old=RenderTexture.active; RenderTexture.active=output;
                image=new Texture2D(output.width,output.height,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,output.width,output.height),0,0); image.Apply();
                int bright=0,blue=0; byte peak=0;
                foreach(var c in image.GetPixels32()) { int top=Mathf.Max(c.r,Mathf.Max(c.g,c.b)); if(top>40)bright++; if(c.b>80 && c.b>c.r*1.3f && c.b>c.g*1.1f)blue++; if(top>peak)peak=(byte)top; }
                Debug.Log($"Minimap probe: copied camera ARGB32 format={output.graphicsFormat} bright={bright} blue={blue} peak={peak}");
                if (icon != null)
                {
                    int originalLayer=icon.gameObject.layer;
                    try
                    {
                        icon.gameObject.layer=31; temp.cullingMask=unchecked((int)0x80000000); temp.useOcclusionCulling=false;
                        temp.clearFlags=CameraClearFlags.SolidColor; temp.backgroundColor=Color.black; temp.Render();
                        image.ReadPixels(new Rect(0,0,output.width,output.height),0,0); image.Apply();
                        bright=0; blue=0; peak=0;
                        foreach(var c in image.GetPixels32()) { int top=Mathf.Max(c.r,Mathf.Max(c.g,c.b)); if(top>40)bright++; if(c.b>80 && c.b>c.r*1.3f && c.b>c.g*1.1f)blue++; if(top>peak)peak=(byte)top; }
                        Debug.Log($"Minimap probe: isolated marker only bright={bright} blue={blue} peak={peak}");
                        if(renderer != null && renderer.sharedMaterial != null)
                        {
                            var originalMaterial=renderer.sharedMaterial; var copy=new Material(originalMaterial); copy.enableInstancing=false;
                            try
                            {
                                renderer.sharedMaterial=copy; temp.Render(); image.ReadPixels(new Rect(0,0,output.width,output.height),0,0); image.Apply();
                                bright=0; blue=0; peak=0;
                                foreach(var c in image.GetPixels32()) {int top=Mathf.Max(c.r,Mathf.Max(c.g,c.b)); if(top>40)bright++; if(c.b>80 && c.b>c.r*1.3f && c.b>c.g*1.1f)blue++; if(top>peak)peak=(byte)top;}
                                Debug.Log($"Minimap probe: isolated marker without instancing bright={bright} blue={blue} peak={peak}");
                                var descriptor=target.descriptor; descriptor.graphicsFormat=target.graphicsFormat;
                                var floatTarget=new RenderTexture(descriptor); Texture2D floatImage=null;
                                try
                                {
                                    temp.targetTexture=floatTarget; temp.Render(); RenderTexture.active=floatTarget;
                                    floatImage=new Texture2D(floatTarget.width,floatTarget.height,TextureFormat.RGBA32,false);
                                    floatImage.ReadPixels(new Rect(0,0,floatTarget.width,floatTarget.height),0,0); floatImage.Apply();
                                    int floatBlue=0; byte floatPeak=0;
                                    foreach(var c in floatImage.GetPixels32()) {int top=Mathf.Max(c.r,Mathf.Max(c.g,c.b)); if(c.b>80 && c.b>c.r*1.3f && c.b>c.g*1.1f)floatBlue++; if(top>floatPeak)floatPeak=(byte)top;}
                                    Debug.Log($"Minimap probe: isolated marker without instancing original-format={floatTarget.graphicsFormat} blue={floatBlue} peak={floatPeak}");
                                }
                                finally { temp.targetTexture=output; RenderTexture.active=output; if(floatImage!=null)Destroy(floatImage); floatTarget.Release(); Destroy(floatTarget); }
                            }
                            finally { renderer.sharedMaterial=originalMaterial; Destroy(copy); }
                            if(originalMaterial.mainTexture!=null)
                            {
                                Graphics.Blit(originalMaterial.mainTexture,output); image.ReadPixels(new Rect(0,0,output.width,output.height),0,0); image.Apply();
                                int opaque=0; peak=0; foreach(var c in image.GetPixels32()) { if(c.a>=128)opaque++; int top=Mathf.Max(c.r,Mathf.Max(c.g,c.b)); if(top>peak)peak=(byte)top; }
                                Debug.Log($"Minimap probe: source texture blit opaque={opaque} peak={peak}");
                            }
                        }
                    }
                    finally { icon.gameObject.layer=originalLayer; }
                }
                RenderTexture.active=old;
            }
            finally { RenderTexture.active=old; if(image!=null)Destroy(image); Destroy(cameraRoot); output.Release(); Destroy(output); }
        }
        Debug.Log("Minimap probe: complete (controlled renders; pixels alone do not prove UI presentation)");
    }
}
#endif
