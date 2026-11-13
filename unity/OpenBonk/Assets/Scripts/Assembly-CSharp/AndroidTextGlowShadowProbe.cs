#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class AndroidTextGlowShadowProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_textglow_shadow_probe", false)) return;
        var go = new GameObject("textglow shadow probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidTextGlowShadowProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || PoolManager.Instance == null || PoolManager.Instance.bonkFx == null)) yield return null;
        if (PoolManager.Instance == null || PoolManager.Instance.bonkFx == null) { Debug.LogError("TextGlow shadow probe INCONCLUSIVE: Bonk prefab unavailable"); yield break; }
        Material source = null;
        foreach (var r in PoolManager.Instance.bonkFx.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials) if (m != null && m.shader != null && m.shader.name == "Archanor VFX/Retro Arsenal/TextGlow") source = m;
        var toonShader = Shader.Find("MK/Toon/Built-in/Standard/Simple + Outline");
        if (source == null || toonShader == null || !source.shader.isSupported || !toonShader.isSupported) { Debug.LogError("TextGlow shadow probe INCONCLUSIVE: materials unavailable"); yield break; }
        var root = new GameObject("isolated shadow scene"); root.transform.position = new Vector3(0, 10000, 0);
        var receiverMaterial = new Material(toonShader);
        receiverMaterial.SetTexture("_AlbedoMap", Texture2D.whiteTexture); receiverMaterial.SetColor("_AlbedoColor", Color.white); receiverMaterial.SetColor("_GoochBrightColor", Color.white); receiverMaterial.SetColor("_GoochDarkColor", Color.black); receiverMaterial.SetFloat("_LightThreshold", .05f); receiverMaterial.SetFloat("_DiffuseSmoothness", .1f); receiverMaterial.SetFloat("_Specular", 0); receiverMaterial.SetFloat("_Rim", 0); receiverMaterial.SetFloat("_Outline", 1); receiverMaterial.SetFloat("_ReceiveShadows", 1);
        var receiver = GameObject.CreatePrimitive(PrimitiveType.Quad); receiver.transform.SetParent(root.transform, false); receiver.transform.localScale = Vector3.one * 3; receiver.layer = 31; receiver.GetComponent<Renderer>().sharedMaterial = receiverMaterial;
        var casterMaterial = new Material(source); var texture = new Texture2D(1,1,TextureFormat.RGBA32,false,true); texture.SetPixel(0,0,Color.white); texture.Apply(); casterMaterial.SetTexture("_TextureSample",texture); casterMaterial.SetTextureScale("_TextureSample",Vector2.one);
        var caster = GameObject.CreatePrimitive(PrimitiveType.Cube); caster.transform.SetParent(root.transform, false); caster.transform.localPosition = new Vector3(.8f,0,-1); caster.transform.localScale = Vector3.one*.6f; caster.layer=31;
        var mesh = Instantiate(caster.GetComponent<MeshFilter>().sharedMesh); var colors = new Color[mesh.vertexCount]; for(int i=0;i<colors.Length;i++) colors[i]=Color.white; mesh.colors=colors; caster.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=caster.GetComponent<Renderer>(); renderer.sharedMaterial=casterMaterial; renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        var cameraObject=new GameObject("textglow camera"); cameraObject.transform.SetParent(root.transform,false); cameraObject.transform.localPosition=new Vector3(0,0,-4); cameraObject.transform.LookAt(root.transform.position);
        var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=2; camera.renderingPath=RenderingPath.Forward; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black; camera.cullingMask=1<<31; camera.enabled=false;
        var lightObject=new GameObject("textglow light"); lightObject.transform.SetParent(root.transform,false); var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.renderMode=LightRenderMode.ForcePixel; light.cullingMask=1<<31; light.transform.rotation=Quaternion.LookRotation(new Vector3(-.8f,0,1)); light.intensity=1; light.shadowStrength=1; light.shadowBias=.01f; light.shadowNormalBias=0;
        var oldMode=RenderSettings.ambientMode; var oldAmbient=RenderSettings.ambientLight; var oldShadows=QualitySettings.shadows; var oldDistance=QualitySettings.shadowDistance; var oldCascades=QualitySettings.shadowCascades;
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=Color.black; QualitySettings.shadows=UnityEngine.ShadowQuality.All; QualitySettings.shadowDistance=30; QualitySettings.shadowCascades=1;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image=new Texture2D(128,128,TextureFormat.RGBA32,false); camera.targetTexture=target;
        yield return null;
        try {
            Color[] Capture() { camera.Render(); var old=RenderTexture.active; RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); RenderTexture.active=old; return image.GetPixels(); }
            light.shadows=LightShadows.None; var reference=Capture(); light.shadows=LightShadows.Hard; var opaque=Capture();
            for(int i=0;i<colors.Length;i++) colors[i]=new Color(1,1,1,0); mesh.colors=colors; var vertexZero=Capture();
            texture.SetPixel(0,0,new Color(1,1,1,0)); texture.Apply(); var transparent=Capture();
            int opaqueDark=0, vertexDark=0, transparentDark=0; for(int i=0;i<reference.Length;i++) { if(reference[i].r-opaque[i].r>.15f) opaqueDark++; if(reference[i].r-vertexZero[i].r>.15f) vertexDark++; if(reference[i].r-transparent[i].r>.15f) transparentDark++; }
            bool pass=opaqueDark>20 && vertexDark>opaqueDark*.8f && transparentDark<opaqueDark*.2f;
            Debug.Log($"TextGlow shadow probe {(pass ? "PASS" : "INCONCLUSIVE")}: opaque={opaqueDark} vertexZero={vertexDark} transparent={transparentDark} material={source.name} api={SystemInfo.graphicsDeviceType}");
        } finally {
            camera.targetTexture=null; RenderTexture.active=null; QualitySettings.shadows=oldShadows; QualitySettings.shadowDistance=oldDistance; QualitySettings.shadowCascades=oldCascades; RenderSettings.ambientMode=oldMode; RenderSettings.ambientLight=oldAmbient;
            Destroy(image); Destroy(target); Destroy(root); Destroy(mesh); Destroy(texture); Destroy(casterMaterial); Destroy(receiverMaterial);
        }
    }
}
#endif
