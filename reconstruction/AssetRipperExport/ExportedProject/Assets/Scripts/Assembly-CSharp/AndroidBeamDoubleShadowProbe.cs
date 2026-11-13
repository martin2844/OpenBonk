#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in synthetic directional shadow check in an isolated Android run.
internal sealed class AndroidBeamDoubleShadowProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_beam_double_shadow_probe",false)) return;
        var go=new GameObject("Beam double shadow probe");DontDestroyOnLoad(go);go.AddComponent<AndroidBeamDoubleShadowProbe>();
    }
    IEnumerator Start() {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || GameManager.Instance==null || !GameManager.Instance.IsPlaying())) yield return null;
        var beamShader=Shader.Find("Archanor VFX/Retro Arsenal/BeamDouble"); var toonShader=Shader.Find("MK/Toon/Built-in/Standard/Simple + Outline");
        if(beamShader==null || toonShader==null || !beamShader.isSupported || !toonShader.isSupported) { Debug.LogError("Beam double shadow probe INCONCLUSIVE: shaders unavailable");yield break; }
        var receiverMaterial=new Material(toonShader);receiverMaterial.SetTexture("_AlbedoMap",Texture2D.whiteTexture);receiverMaterial.SetColor("_AlbedoColor",Color.white);receiverMaterial.SetColor("_GoochBrightColor",Color.white);receiverMaterial.SetColor("_GoochDarkColor",Color.black);receiverMaterial.SetFloat("_LightThreshold",.05f);receiverMaterial.SetFloat("_DiffuseSmoothness",.1f);receiverMaterial.SetFloat("_Specular",0);receiverMaterial.SetFloat("_Rim",0);receiverMaterial.SetFloat("_Outline",1);receiverMaterial.SetFloat("_ReceiveShadows",1);
        Vector3 origin=new Vector3(0,10000,0);var receiver=GameObject.CreatePrimitive(PrimitiveType.Quad);receiver.layer=31;receiver.transform.position=origin;receiver.transform.localScale=Vector3.one*3;receiver.GetComponent<Renderer>().sharedMaterial=receiverMaterial;
        var casterMaterial=new Material(beamShader);var texture=new Texture2D(1,1,TextureFormat.RGBA32,false,true);texture.SetPixel(0,0,Color.white);texture.Apply();casterMaterial.SetTexture("_TextureSample",texture);casterMaterial.SetTextureScale("_TextureSample",Vector2.one);casterMaterial.SetVector("_ScrollSpeed",Vector4.zero);var texture2=new Texture2D(1,1,TextureFormat.RGBA32,false,true);texture2.SetPixel(0,0,Color.white);texture2.Apply();casterMaterial.SetTexture("_TextureSample2",texture2);casterMaterial.SetTextureScale("_TextureSample2",Vector2.one);casterMaterial.SetVector("_ScrollSpeed2",Vector4.zero);
        var caster=GameObject.CreatePrimitive(PrimitiveType.Cube);caster.layer=31;caster.transform.position=origin+new Vector3(.8f,0,-1);caster.transform.localScale=Vector3.one*.6f;
        var mesh=Instantiate(caster.GetComponent<MeshFilter>().sharedMesh);var colors=new Color[mesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;caster.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=caster.GetComponent<Renderer>();renderer.sharedMaterial=casterMaterial;renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        var cameraObject=new GameObject("Beam double shadow camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=origin+Vector3.back*4;camera.transform.LookAt(origin);camera.orthographic=true;camera.orthographicSize=2;camera.renderingPath=RenderingPath.Forward;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var lightObject=new GameObject("Beam double shadow light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;light.renderMode=LightRenderMode.ForcePixel;light.transform.rotation=Quaternion.LookRotation(new Vector3(-.8f,0,1));light.intensity=1;light.shadowStrength=1;light.shadowBias=.01f;light.shadowNormalBias=0;
        var oldMode=RenderSettings.ambientMode;var oldAmbient=RenderSettings.ambientLight;var oldFog=RenderSettings.fog;var oldShadows=QualitySettings.shadows;var oldDistance=QualitySettings.shadowDistance;var oldCascades=QualitySettings.shadowCascades;
        RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=30;QualitySettings.shadowCascades=1;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);var image=new Texture2D(128,128,TextureFormat.RGBA32,false);camera.targetTexture=target;
        try {
            Color[] Capture(){camera.Render();var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;return image.GetPixels();}
            light.shadows=LightShadows.None;var reference=Capture();light.shadows=LightShadows.Hard;var opaque=Capture();texture.SetPixel(0,0,new Color(1,1,1,0));texture.Apply();var transparent=Capture();texture.SetPixel(0,0,Color.white);texture.Apply();texture2.SetPixel(0,0,new Color(1,1,1,0));texture2.Apply();var transparent2=Capture();
            int opaqueDark=0,transparentDark=0,transparent2Dark=0;for(int i=0;i<reference.Length;i++){if(reference[i].r-opaque[i].r>.15f)opaqueDark++;if(reference[i].r-transparent[i].r>.15f)transparentDark++;if(reference[i].r-transparent2[i].r>.15f)transparent2Dark++;}
            Debug.Log($"Beam double shadow probe {(opaqueDark>20 && transparentDark<opaqueDark*.2f && transparent2Dark<opaqueDark*.2f?"PASS":"FAIL")}: opaque={opaqueDark}, transparent={transparentDark}, transparent2={transparent2Dark}, shaderLevel={SystemInfo.graphicsShaderLevel}");
        } finally {camera.targetTexture=null;RenderTexture.active=null;QualitySettings.shadows=oldShadows;QualitySettings.shadowDistance=oldDistance;QualitySettings.shadowCascades=oldCascades;RenderSettings.fog=oldFog;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;Destroy(image);Destroy(target);Destroy(cameraObject);Destroy(lightObject);Destroy(caster);Destroy(mesh);Destroy(texture);Destroy(texture2);Destroy(casterMaterial);Destroy(receiver);Destroy(receiverMaterial);}
    }
}
#endif
