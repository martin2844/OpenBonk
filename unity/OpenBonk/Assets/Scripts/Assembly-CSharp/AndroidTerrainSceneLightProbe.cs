#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Opt-in renderer diagnostic. Scene/time/settings are restored after four manual renders.
internal sealed class AndroidTerrainSceneLightProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra","megabonk_terrain_scene_light_probe",false)) return;
        var go=new GameObject("Terrain scene light probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidTerrainSceneLightProbe>();
    }
    static Color32[] Capture(Camera camera,RenderTexture rt,Texture2D image)
    {
        camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply(); return image.GetPixels32();
    }
    static int Difference(Color32[] a,Color32[] b)
    {
        int count=0; for(int i=0;i<a.Length;i++) if (Mathf.Abs(a[i].r-b[i].r)>3 || Mathf.Abs(a[i].g-b[i].g)>3 || Mathf.Abs(a[i].b-b[i].b)>3) count++; return count;
    }
    IEnumerator Start()
    {
        float until=Time.realtimeSinceStartup+180;
        while(Time.realtimeSinceStartup<until && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || Camera.main==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying())) yield return null;
        if(MyPlayer.Instance==null || Camera.main==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying()) { Debug.LogError("Scene light probe INCONCLUSIVE: gameplay unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3);
        var camera=Camera.main; var player=MyPlayer.Instance; var lights=FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.enabled && x.gameObject.activeInHierarchy).ToArray();
        int directional=lights.Count(x=>x.type==LightType.Directional), point=lights.Count(x=>x.type==LightType.Point), spot=lights.Count(x=>x.type==LightType.Spot), forced=lights.Count(x=>x.renderMode==LightRenderMode.ForcePixel);
        int defaultCount=QualitySettings.pixelLightCount, quality=QualitySettings.GetQualityLevel();
        var nearby=lights.Where(x=>x.type!=LightType.Directional && Vector3.Distance(x.transform.position,player.transform.position)<=x.range).ToArray();
        var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active; var oldTime=Time.timeScale;
        var target=new RenderTexture(480,270,24,RenderTextureFormat.ARGB32); var image=new Texture2D(480,270,TextureFormat.RGBA32,false);
        var lampObject=new GameObject("Scene light probe lamp"); var lamp=lampObject.AddComponent<Light>(); lamp.type=LightType.Point; lamp.renderMode=LightRenderMode.Auto; lamp.color=Color.red; lamp.intensity=4; lamp.range=18; lamp.transform.position=player.transform.position+Vector3.up*3; lamp.enabled=false;
        Time.timeScale=0;
        yield return null;
        var naturalDefault=Capture(camera,target,image);
        yield return null;
        var naturalRepeat=Capture(camera,target,image);
        QualitySettings.pixelLightCount=4;
        yield return null;
        var naturalFour=Capture(camera,target,image);
        lamp.enabled=true; QualitySettings.pixelLightCount=defaultCount;
        yield return null;
        var injectedDefault=Capture(camera,target,image);
        QualitySettings.pixelLightCount=4;
        yield return null;
        var injectedFour=Capture(camera,target,image);
        Debug.Log($"Scene light probe RESULT: quality={quality} defaultPixelLights={defaultCount} activeDirectional={directional} activePoint={point} activeSpot={spot} forcePixel={forced} nearbyNonDirectional={nearby.Length} naturalRepeat={Difference(naturalDefault,naturalRepeat)} natural1v4={Difference(naturalDefault,naturalFour)} injected1v4={Difference(injectedDefault,injectedFour)} injectedAtDefault={Difference(naturalDefault,injectedDefault)} injectedAtFour={Difference(naturalFour,injectedFour)}");
        QualitySettings.pixelLightCount=defaultCount; Time.timeScale=oldTime; camera.targetTexture=oldTarget; RenderTexture.active=oldActive; target.Release(); Destroy(image); Destroy(target); Destroy(lampObject);
    }
}
#endif
