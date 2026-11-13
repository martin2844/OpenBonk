#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in diagnostic; no scene, material or player state is modified.
internal sealed class AndroidForceFieldInstancingProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_forcefield_instancing_probe", false)) return;
        var root = new GameObject("forcefield instancing probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidForceFieldInstancingProbe>();
    }
    IEnumerator Start()
    {
        Material original=null; float deadline=Time.realtimeSinceStartup+180f;
        while (Time.realtimeSinceStartup<deadline && original==null)
        {
            if (MyPlayer.Instance != null && MyPlayer.Instance.hasStarted)
                foreach (var candidate in Resources.FindObjectsOfTypeAll<Material>())
                    if (candidate != null && candidate.name == "WarningField" && candidate.shader != null && candidate.shader.name == "Kaima/Depth/ForceField") { original=candidate; break; }
            if (original==null) yield return null;
        }
        if (original==null || !SystemInfo.supportsInstancing || !original.shader.isSupported)
        { Debug.Log("ForceField instancing Android INCONCLUSIVE: material="+(original!=null)+" instancing="+SystemInfo.supportsInstancing); yield break; }
        var origin=Vector3.zero;
        var root=new GameObject("forcefield controlled camera");
        var camera=root.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=1f; camera.transform.position=origin+new Vector3(0,0,-5); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black; camera.cullingMask=1<<30; camera.allowHDR=false; camera.allowMSAA=false;
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=quad.GetComponent<MeshFilter>().sharedMesh; quad.SetActive(false);
        var material=new Material(original); material.enableInstancing=true; material.SetFloat("_RimPower",0f); material.SetFloat("_IntersectionPower",0f);
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image=new Texture2D(128,128,TextureFormat.RGBA32,false,true); camera.targetTexture=target;
        var props=new MaterialPropertyBlock(); props.SetVectorArray("_MainColor",new[]{new Vector4(1,0,0,1),new Vector4(0,1,0,1)});
        var matrices=new[]{Matrix4x4.Translate(origin+new Vector3(-.5f,0,0)),Matrix4x4.Translate(origin+new Vector3(.5f,0,0))};
        var commands=new CommandBuffer();
        try
        {
            var controlRed=new MaterialPropertyBlock(); controlRed.SetColor("_MainColor",Color.red);
            var controlGreen=new MaterialPropertyBlock(); controlGreen.SetColor("_MainColor",Color.green);
            commands.DrawMesh(mesh,matrices[0],material,0,0,controlRed); commands.DrawMesh(mesh,matrices[1],material,0,0,controlGreen);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); camera.Render();
            RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var controlLeft=image.GetPixel(32,64); var controlRight=image.GetPixel(96,64);
            Debug.Log($"ForceField Android control: gpu={SystemInfo.graphicsDeviceType}, left={controlLeft}, right={controlRight}");
            System.IO.File.WriteAllBytes(Application.persistentDataPath+"/forcefield-control.png",image.EncodeToPNG());
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); commands.Clear();
            material.SetColor("_MainColor",Color.yellow);
            commands.DrawMeshInstanced(mesh,0,material,0,matrices,2,null); camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); camera.Render();
            RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var sharedLeft=image.GetPixel(32,64); var sharedRight=image.GetPixel(96,64);
            Debug.Log($"ForceField Android shared instance color: left={sharedLeft}, right={sharedRight}");
            System.IO.File.WriteAllBytes(Application.persistentDataPath+"/forcefield-instancing-shared.png",image.EncodeToPNG());
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); commands.Clear();
            commands.DrawMeshInstanced(mesh,0,material,0,matrices,2,props); camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,commands);
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var left=image.GetPixel(32,64); var right=image.GetPixel(96,64);
            bool controlOk=controlLeft.r>.75f && controlLeft.g<.1f && controlRight.g>.75f && controlRight.r<.1f;
            bool ok=left.r>.75f && left.g<.1f && right.g>.75f && right.r<.1f;
            Debug.Log($"ForceField instancing Android {(!controlOk ? "INCONCLUSIVE" : ok ? "PASS" : "FAIL")}: material={original.name}, gpu={SystemInfo.graphicsDeviceType}, left={left}, right={right}, controlOk={controlOk}, sourceInstancing={original.enableInstancing}");
            System.IO.File.WriteAllBytes(Application.persistentDataPath+"/forcefield-instancing.png",image.EncodeToPNG());
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); commands.Clear();
            var comparisonShader=Shader.Find("Custom/HitFlash_BuiltIn");
            if (comparisonShader == null || !comparisonShader.isSupported) Debug.Log("ForceField Android comparison INCONCLUSIVE: HitFlash shader unavailable");
            else
            {
                var comparison=new Material(comparisonShader); comparison.enableInstancing=true; comparison.SetTexture("_MainTex",Texture2D.whiteTexture); comparison.SetFloat("_FlashAmount",0f); comparison.SetColor("_Color",Color.yellow);
                var comparisonProps=new MaterialPropertyBlock(); comparisonProps.SetVectorArray("_Color",new[]{new Vector4(1,0,0,1),new Vector4(0,1,0,1)}); comparisonProps.SetFloatArray("_FlashAmount",new[]{0f,0f});
                commands.DrawMeshInstanced(mesh,0,comparison,0,matrices,2,comparisonProps); camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); camera.Render();
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
                var compareLeft=image.GetPixel(32,64); var compareRight=image.GetPixel(96,64);
                Debug.Log($"ForceField Android comparison HitFlash: left={compareLeft}, right={compareRight}");
                System.IO.File.WriteAllBytes(Application.persistentDataPath+"/forcefield-hitflash-comparison.png",image.EncodeToPNG());
                camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); commands.Clear(); Destroy(comparison);
            }
            Graphics.DrawMeshInstanced(mesh,0,material,matrices,2,props,ShadowCastingMode.Off,false,30,camera);
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var graphicsLeft=image.GetPixel(32,64); var graphicsRight=image.GetPixel(96,64);
            Debug.Log($"ForceField Android Graphics.DrawMeshInstanced: left={graphicsLeft}, right={graphicsRight}");
            System.IO.File.WriteAllBytes(Application.persistentDataPath+"/forcefield-graphics-instanced.png",image.EncodeToPNG());
            var rendererLeft=GameObject.CreatePrimitive(PrimitiveType.Quad);
            var rendererRight=GameObject.CreatePrimitive(PrimitiveType.Quad);
            rendererLeft.layer=30; rendererRight.layer=30;
            rendererLeft.transform.position=origin+new Vector3(-.5f,0,0); rendererRight.transform.position=origin+new Vector3(.5f,0,0);
            var leftRenderer=rendererLeft.GetComponent<MeshRenderer>(); var rightRenderer=rendererRight.GetComponent<MeshRenderer>();
            leftRenderer.sharedMaterial=material; rightRenderer.sharedMaterial=material;
            leftRenderer.SetPropertyBlock(controlRed); rightRenderer.SetPropertyBlock(controlGreen);
            material.enableInstancing=false;
            yield return null;
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var rendererControlLeft=image.GetPixel(32,64); var rendererControlRight=image.GetPixel(96,64);
            Debug.Log($"ForceField Android MeshRenderer control: left={rendererControlLeft}, right={rendererControlRight}, instancingEnabled={material.enableInstancing}");
            material.enableInstancing=true;
            yield return null;
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var rendererLeftColor=image.GetPixel(32,64); var rendererRightColor=image.GetPixel(96,64);
            Debug.Log($"ForceField Android MeshRenderer: left={rendererLeftColor}, right={rendererRightColor}, instancingEnabled={material.enableInstancing}");
            System.IO.File.WriteAllBytes(Application.persistentDataPath+"/forcefield-renderers.png",image.EncodeToPNG());
            var glowShader=Shader.Find("Archanor VFX/Retro Arsenal/BasicGlow");
            if (glowShader != null && glowShader.isSupported)
            {
                var glow=new Material(glowShader); glow.SetTexture("_TextureSample",Texture2D.whiteTexture); glow.SetColor("_Tint",Color.white); glow.SetFloat("_ExtraGlow",1f);
                leftRenderer.sharedMaterial=glow; rightRenderer.sharedMaterial=glow;
                leftRenderer.SetPropertyBlock(null); rightRenderer.SetPropertyBlock(null);
                bool wasFog=RenderSettings.fog; RenderSettings.fog=false;
                glow.enableInstancing=false; yield return null;
                camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
                var glowControlLeft=image.GetPixel(32,64); var glowControlRight=image.GetPixel(96,64);
                glow.enableInstancing=true; yield return null;
                camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
                var glowInstancedLeft=image.GetPixel(32,64); var glowInstancedRight=image.GetPixel(96,64);
                Debug.Log($"BasicGlow Android MeshRenderer: disabled={glowControlLeft}/{glowControlRight}, enabled={glowInstancedLeft}/{glowInstancedRight}");
                RenderSettings.fog=wasFog; Destroy(glow);
            }
            else Debug.Log("BasicGlow Android MeshRenderer INCONCLUSIVE: shader unavailable");
            Destroy(rendererLeft); Destroy(rendererRight);
        }
        finally
        {
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); commands.Release(); RenderTexture.active=null; camera.targetTexture=null;
            Destroy(image); target.Release(); Destroy(target); Destroy(quad); Destroy(material); Destroy(root); Destroy(gameObject);
        }
    }
}
#endif
