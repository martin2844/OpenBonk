#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.Rendering;

// Exercises the real enemy dissolve material with two forced instance values.
internal sealed class AndroidDissolveInstancingProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_dissolve_instancing_probe", false)) return;
        var root = new GameObject("dissolve instancing probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidDissolveInstancingProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        Material source=null;
        foreach(var m in Resources.FindObjectsOfTypeAll<Material>()) if(m.name=="m_disintegration") { source=m; break; }
        if(source==null || !source.shader.isSupported || !SystemInfo.supportsInstancing) { Debug.LogError($"Dissolve instancing probe FAIL: source={source}, instancing={SystemInfo.supportsInstancing}"); yield break; }
        var material=new Material(source); material.enableInstancing=true;
        var noise=new Texture2D(1,1,TextureFormat.RGBA32,false,true); noise.SetPixel(0,0,new Color(.5f,0,0,1)); noise.Apply();
        material.SetTexture("_NoiseTexture",noise); material.SetColor("_Color",Color.white);
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=primitive.GetComponent<MeshFilter>().sharedMesh; primitive.SetActive(false);
        var camGo=new GameObject("dissolve probe camera"); camGo.transform.position=new Vector3(0,10000,-4); var camera=camGo.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=1; camera.aspect=1; camera.cullingMask=1<<30; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.blue; camera.allowHDR=false; camera.allowMSAA=false;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image=new Texture2D(128,128,TextureFormat.RGBA32,false); camera.targetTexture=target;
        var matrices=new[]{Matrix4x4.TRS(new Vector3(-.5f,10000,0),Quaternion.identity,Vector3.one*.7f),Matrix4x4.TRS(new Vector3(.5f,10000,0),Quaternion.identity,Vector3.one*.7f)};
        var block=new MaterialPropertyBlock(); block.SetFloatArray("_DisplacementStrength",new[]{0f,0f});
        var previous=RenderTexture.active; bool oldFog=RenderSettings.fog; RenderSettings.fog=false;
        primitive.layer=30; primitive.transform.position=new Vector3(0,10000,0); primitive.GetComponent<Renderer>().sharedMaterial=material; primitive.SetActive(true);
        material.SetFloat("_DissolveAmount",0); material.SetFloat("_DisplacementStrength",0);
        yield return null; // Allow the renderer to enter Unity's culling lists.
        var baselineShader=Shader.Find("Custom/UIRainbowShader");
        Material baselineMaterial=baselineShader != null ? new Material(baselineShader) : null;
        Color baseline=Color.clear;
        if (baselineMaterial != null) {
            baselineMaterial.SetFloat("_Speed",0); primitive.GetComponent<Renderer>().sharedMaterial=baselineMaterial;
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); baseline=image.GetPixel(64,64);
            primitive.GetComponent<Renderer>().sharedMaterial=material;
        }
        var dissolveComponent=primitive.AddComponent<EnemyDissolve>(); dissolveComponent.dissolveMaterial=material; dissolveComponent.Reset();
        bool resetDisabledInstancing=!material.enableInstancing;
        camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var noInstancing=image.GetPixel(64,64);
        material.SetFloat("_DissolveAmount",-1f);
        camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var negativeAmount=image.GetPixel(64,64);
        material.SetFloat("_DissolveAmount",0);
        primitive.transform.position=new Vector3(-.5f,10000,0); primitive.transform.localScale=Vector3.one*.7f;
        var second=GameObject.CreatePrimitive(PrimitiveType.Quad); second.layer=30; second.transform.position=new Vector3(.5f,10000,0); second.transform.localScale=Vector3.one*.7f; second.GetComponent<Renderer>().sharedMaterial=material;
        var leftBlock=new MaterialPropertyBlock(); var rightBlock=new MaterialPropertyBlock();
        leftBlock.SetFloat("_DissolveAmount",0); rightBlock.SetFloat("_DissolveAmount",1);
        primitive.GetComponent<Renderer>().SetPropertyBlock(leftBlock); second.GetComponent<Renderer>().SetPropertyBlock(rightBlock);
        yield return null;
        camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var fallbackLeft=image.GetPixel(32,64); var fallbackRight=image.GetPixel(96,64);
        leftBlock.SetFloat("_DissolveAmount",1); rightBlock.SetFloat("_DissolveAmount",0);
        primitive.GetComponent<Renderer>().SetPropertyBlock(leftBlock); second.GetComponent<Renderer>().SetPropertyBlock(rightBlock);
        camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var fallbackSwappedLeft=image.GetPixel(32,64); var fallbackSwappedRight=image.GetPixel(96,64);
        Destroy(second); primitive.transform.position=new Vector3(0,10000,0); primitive.transform.localScale=Vector3.one; primitive.GetComponent<Renderer>().SetPropertyBlock(null); material.enableInstancing=true;
        camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var control=image.GetPixel(64,64);
        primitive.SetActive(false);
        Color[] Render(float left, float right) {
            block.SetFloatArray("_DissolveAmount",new[]{left,right});
            var commands=new CommandBuffer { name="dissolve instancing test" };
            commands.DrawMeshInstanced(mesh,0,material,0,matrices,2,block);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,commands);
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); commands.Release();
            return new[]{image.GetPixel(32,64),image.GetPixel(96,64)};
        }
        yield return null;
        var a=Render(0,1); var b=Render(1,0);
        bool White(Color c)=>c.r>.9f && c.g>.9f && c.b>.9f;
        bool Blue(Color c)=>c.r<.1f && c.g<.1f && c.b>.9f;
        bool ok=White(control) && White(a[0]) && Blue(a[1]) && Blue(b[0]) && White(b[1]);
        bool fallbackOk=resetDisabledInstancing && White(fallbackLeft) && Blue(fallbackRight) && Blue(fallbackSwappedLeft) && White(fallbackSwappedRight);
        Debug.Log($"Dissolve instancing probe {(ok?"PASS":"FAIL")}: baselineShader={baselineShader}, baseline={baseline}, nonInstanced={noInstancing}, negativeAmount={negativeAmount}, control={control}, first={a[0]}/{a[1]}, swapped={b[0]}/{b[1]}, resetDisabledInstancing={resetDisabledInstancing}, fallback={(fallbackOk?"PASS":"FAIL")} {fallbackLeft}/{fallbackRight} swapped={fallbackSwappedLeft}/{fallbackSwappedRight}, shader={material.shader.name}");
        RenderSettings.fog=oldFog; RenderTexture.active=previous; camera.targetTexture=null; target.Release(); Destroy(target); Destroy(image); Destroy(camGo); Destroy(primitive); Destroy(material); Destroy(noise); if (baselineMaterial != null) Destroy(baselineMaterial);
    }
}
#endif
