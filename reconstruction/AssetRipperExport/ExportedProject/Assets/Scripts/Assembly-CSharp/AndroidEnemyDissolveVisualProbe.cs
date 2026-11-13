#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Draw a copy of the first naturally defeated enemy with its real dissolve
// material and noise texture at controlled amounts; isolated test intent only.
internal sealed class AndroidEnemyDissolveVisualProbe : MonoBehaviour
{
    bool started;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_enemy_dissolve_visual_probe",false)) return;
        var go=new GameObject("enemy dissolve visual probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidEnemyDissolveVisualProbe>();
    }
    void OnEnable()=>Enemy.A_EnemyDied+=OnDeath;
    void OnDisable()=>Enemy.A_EnemyDied-=OnDeath;
    void OnDeath(Enemy enemy, DamageContainer dc)
    {
        if(started || enemy==null || enemy.dissolve==null || enemy.dissolve.enemyRenderer==null) return;
        started=true; StartCoroutine(RenderEnemy(enemy));
    }
    IEnumerator RenderEnemy(Enemy enemy)
    {
        var source=enemy.dissolve.enemyRenderer; var material=source.sharedMaterial;
        Mesh mesh=null; bool ownsMesh=false;
        if(source is SkinnedMeshRenderer skin) { mesh=new Mesh(); skin.BakeMesh(mesh); ownsMesh=true; }
        else { var filter=source.GetComponent<MeshFilter>(); if(filter!=null) mesh=filter.sharedMesh; }
        if(mesh==null || material==null) { Debug.LogError($"Enemy dissolve visual FAIL: mesh={mesh}, material={material}, renderer={source.GetType().Name}"); yield break; }
        Bounds bounds=mesh.bounds; float extent=Mathf.Max(bounds.extents.x,bounds.extents.y);
        if(extent<.001f) { Debug.LogError("Enemy dissolve visual FAIL: empty mesh bounds"); yield break; }
        const int layer=30; var go=new GameObject("isolated enemy dissolve mesh"); go.layer=layer; go.transform.position=new Vector3(0,10000,0)-bounds.center;
        go.AddComponent<MeshFilter>().sharedMesh=mesh; var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        var camGo=new GameObject("isolated dissolve camera"); camGo.transform.position=new Vector3(0,10000,-bounds.extents.z-4); var camera=camGo.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=extent*1.35f; camera.aspect=1; camera.cullingMask=1<<layer; camera.nearClipPlane=.1f; camera.farClipPlane=100; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.blue; camera.allowHDR=false; camera.allowMSAA=false;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image=new Texture2D(128,128,TextureFormat.RGBA32,false); camera.targetTexture=target; var previous=RenderTexture.active; bool oldFog=RenderSettings.fog; RenderSettings.fog=false;
        var properties=new MaterialPropertyBlock();
        int Render(float amount) {
            properties.SetFloat("_DissolveAmount",amount); properties.SetFloat("_DisplacementStrength",0); renderer.SetPropertyBlock(properties);
            camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); int count=0;
            foreach(var p in image.GetPixels32()) if(p.r>32 && p.g>32) count++;
            return count;
        }
        yield return null;
        int full=Render(0), early=Render(.2f), third=Render(.3f), near=Render(.4f), mid=Render(.5f), end=Render(1);
        var noise=material.GetTexture("_NoiseTexture");
        bool ok=full>50 && mid>0 && mid<full && end<mid && !material.enableInstancing;
        Debug.Log($"Enemy dissolve visual {(ok?"PASS":"FAIL")}: enemy={enemy.name}, mesh={mesh.name}, shader={material.shader.name}, instancing={material.enableInstancing}, colorSpace={QualitySettings.activeColorSpace}, noise={(noise!=null?noise.name:"null")}, format={(noise!=null?noise.graphicsFormat.ToString():"none")}, sRGB={(noise!=null && GraphicsFormatUtility.IsSRGBFormat(noise.graphicsFormat))}, filter={(noise!=null?noise.filterMode.ToString():"none")}, opaquePixels_0_02_03_04_05_1={full}/{early}/{third}/{near}/{mid}/{end}");
        RenderSettings.fog=oldFog; RenderTexture.active=previous; camera.targetTexture=null; target.Release(); Destroy(target); Destroy(image); Destroy(camGo); Destroy(go); if(ownsMesh) Destroy(mesh);
    }
}
#endif
