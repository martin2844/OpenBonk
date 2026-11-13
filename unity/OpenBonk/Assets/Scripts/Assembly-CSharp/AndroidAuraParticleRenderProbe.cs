#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using UnityEngine;

// Opt-in rendering diagnostic; grants Aura in a disposable run.
internal sealed class AndroidAuraParticleRenderProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_aura_particle_render_probe",false)) return;
        var go=new GameObject("Aura particle render probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidAuraParticleRenderProbe>();
    }
    IEnumerator Start() {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || DataManager.Instance==null)) yield return null;
        var player=MyPlayer.Instance;
        if(player==null || player.inventory==null || DataManager.Instance==null) { Debug.LogError("Aura particle render probe INCONCLUSIVE: player unavailable"); yield break; }
        var data=DataManager.Instance.GetWeapon(EWeapon.Aura);
        if(data==null) { Debug.LogError("Aura particle render probe INCONCLUSIVE: Aura weapon unavailable"); yield break; }
        player.inventory.weaponInventory.AddWeapon(data,null);
        CombatAura aura=null; ParticleSystemRenderer renderer=null; ParticleSystem particles=null;
        deadline=Time.realtimeSinceStartup+30f;
        while(Time.realtimeSinceStartup<deadline) {
            aura=FindObjectOfType<CombatAura>();
            if(aura!=null) {
                foreach(var candidate in aura.GetComponentsInChildren<ParticleSystemRenderer>(true)) {
                    var material=candidate.sharedMaterial;
                    if(material!=null && material.name=="AuraCircle") { renderer=candidate; particles=candidate.GetComponent<ParticleSystem>(); break; }
                }
                if(renderer!=null && particles!=null && particles.particleCount>0) break;
            }
            yield return null;
        }
        if(aura==null || renderer==null || particles==null || particles.particleCount==0) { Debug.LogError("Aura particle render probe INCONCLUSIVE: active mesh particles unavailable"); yield break; }
        var scaleTimer=typeof(CombatAura).GetField("scaleTimer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        deadline=Time.realtimeSinceStartup+25f;
        while(Time.realtimeSinceStartup<deadline && (float)scaleTimer.GetValue(aura)<.99f) yield return null;
        if ((float)scaleTimer.GetValue(aura)<.99f) { Debug.LogError("Aura particle render probe INCONCLUSIVE: scale animation did not finish"); yield break; }
        bool initialInstancing=renderer.enableGPUInstancing, wasEnabled=renderer.enabled, wasFog=RenderSettings.fog;
        int oldLayer=renderer.gameObject.layer, count=particles.particleCount;
        var cameraObject=new GameObject("Aura particle render camera"); var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.orthographic=true; camera.orthographicSize=Mathf.Max(3f,aura.transform.lossyScale.x*1.4f);
        cameraObject.transform.position=aura.transform.position+new Vector3(0,20,-8); cameraObject.transform.LookAt(aura.transform.position);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.blue; camera.cullingMask=1<<31; camera.allowHDR=false; camera.allowMSAA=false;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); var image=new Texture2D(256,256,TextureFormat.RGBA32,false); camera.targetTexture=target;
        particles.Pause(true); renderer.gameObject.layer=31; RenderSettings.fog=false;
        try {
            Color32[] Capture() { camera.Render(); var old=RenderTexture.active; RenderTexture.active=target; image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active=old; return image.GetPixels32(); }
            renderer.enabled=false; var background=Capture(); renderer.enabled=true;
            renderer.enableGPUInstancing=false; var normal=Capture(); renderer.enableGPUInstancing=true; var instanced=Capture();
            int visibleNormal=0,visibleInstanced=0,different=0;
            for(int i=0;i<normal.Length;i++) {
                int Difference(Color32 a,Color32 b)=>System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b);
                if(Difference(normal[i],background[i])>12) visibleNormal++;
                if(Difference(instanced[i],background[i])>12) visibleInstanced++;
                if(Difference(normal[i],instanced[i])>12) different++;
            }
            bool pass=visibleNormal>30 && visibleInstanced>30;
            Debug.Log($"Aura particle render probe {(pass?"PASS":"FAIL")}: material={renderer.sharedMaterial.name}, shader={renderer.sharedMaterial.shader.name}, meshMode={renderer.renderMode}, particles={count}, scale={aura.transform.lossyScale}, cameraSize={camera.orthographicSize:F2}, authoredInstancing={initialInstancing}, normalPixels={visibleNormal}, instancedPixels={visibleInstanced}, pixelDifference={different}");
        } finally {
            renderer.enableGPUInstancing=initialInstancing; renderer.enabled=wasEnabled; renderer.gameObject.layer=oldLayer; RenderSettings.fog=wasFog;
            particles.Play(true); camera.targetTexture=null; Destroy(cameraObject); Destroy(image); Destroy(target);
        }
    }
}
#endif
