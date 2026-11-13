#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Opt-in controlled render of an authored Frost Walker particle prefab.
internal sealed class AndroidFrostParticleProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_frost_particle_probe",false)) return;
        var go=new GameObject("Frost particle probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidFrostParticleProbe>();
    }

    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || DataManager.Instance==null)) yield return null;
        var data=DataManager.Instance==null ? null : DataManager.Instance.GetWeapon(EWeapon.Frostwalker);
        if(data==null || data.attack==null) { Debug.Log("Frost particle probe INCONCLUSIVE: prefab unavailable"); yield break; }
        var position=new Vector3(0,10000,0);
        var effect=Instantiate(data.attack,position,Quaternion.identity);
        foreach(var behaviour in effect.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled=false;
        var all=effect.GetComponentsInChildren<ParticleSystemRenderer>(true);
        ParticleSystemRenderer selected=null;
        foreach(var renderer in all)
        {
            var material=renderer.sharedMaterial;
            if(selected==null && renderer.renderMode==ParticleSystemRenderMode.Mesh && material!=null && material.shader!=null && material.shader.name=="Archanor VFX/Retro Arsenal/BasicGlow") selected=renderer;
            renderer.enabled=false;
        }
        if(selected==null) { Debug.Log("Frost particle probe INCONCLUSIVE: mesh renderer unavailable"); Destroy(effect); yield break; }
        var particles=selected.GetComponent<ParticleSystem>();
        selected.gameObject.SetActive(true); selected.gameObject.layer=31; selected.enabled=true;
        particles.Simulate(.15f,false,true,true); particles.Pause(false);
        var cameraObject=new GameObject("Frost probe camera"); var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.orthographic=true; camera.orthographicSize=8f;
        cameraObject.transform.position=position+new Vector3(0,15,-10); cameraObject.transform.LookAt(position);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.blue; camera.cullingMask=1<<31; camera.allowHDR=false; camera.allowMSAA=false;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); var image=new Texture2D(256,256,TextureFormat.RGBA32,false); camera.targetTexture=target;
        bool fog=RenderSettings.fog; RenderSettings.fog=false;
        yield return null;
        try
        {
            Color32[] Capture() { camera.Render(); var old=RenderTexture.active; RenderTexture.active=target; image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active=old; return image.GetPixels32(); }
            selected.enabled=false; var background=Capture(); selected.enabled=true;
            selected.enableGPUInstancing=false; var normal=Capture(); selected.enableGPUInstancing=true; var instanced=Capture();
            int visibleNormal=0,visibleInstanced=0,different=0;
            for(int i=0;i<normal.Length;i++)
            {
                int Diff(Color32 a,Color32 b)=>System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b);
                if(Diff(normal[i],background[i])>12) visibleNormal++;
                if(Diff(instanced[i],background[i])>12) visibleInstanced++;
                if(Diff(normal[i],instanced[i])>12) different++;
            }
            Debug.Log($"Frost particle probe: renderer={selected.name}, material={selected.sharedMaterial.name}, particles={particles.particleCount}, normalPixels={visibleNormal}, instancedPixels={visibleInstanced}, difference={different}");
        }
        finally { RenderSettings.fog=fog; camera.targetTexture=null; Destroy(effect); Destroy(cameraObject); Destroy(image); target.Release(); Destroy(target); Destroy(gameObject); }
    }
}
#endif
