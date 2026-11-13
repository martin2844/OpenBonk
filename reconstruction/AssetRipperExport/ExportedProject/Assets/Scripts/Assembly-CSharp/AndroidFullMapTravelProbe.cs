#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.InGame.FullMap;
using UnityEngine;

// Opt-in observer for a separate scripted walking diagnostic.
internal sealed class AndroidFullMapTravelProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_fullmap_travel_probe",false)) return;
        var go=new GameObject("Full map travel probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidFullMapTravelProbe>();
    }
    IEnumerator Start()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        bool inputMode=intent.Call<bool>("getBooleanExtra","megabonk_fullmap_travel_input_probe",false);
        var field=typeof(FullMap).GetField("pixels",BindingFlags.Instance|BindingFlags.NonPublic);
        FullMap map=null; float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline)
        {
            map=FindFirstObjectByType<FullMap>();
            if(map!=null && field.GetValue(map)!=null && MyPlayer.Instance!=null && MyPlayer.Instance.hasStarted) break;
            yield return null;
        }
        if(map==null || field.GetValue(map)==null || MyPlayer.Instance==null) { Debug.LogError("Full map travel INCONCLUSIVE: map or player unavailable"); yield break; }
        if(!inputMode) FullMapUi.A_Toggle?.Invoke(true);
        yield return new WaitForSecondsRealtime(2f);
        var initial=(Color32[])((Color32[])field.GetValue(map)).Clone();
        Vector3 start=MyPlayer.Instance.transform.position;
        deadline=Time.realtimeSinceStartup+180f;
        float distance=0; int increased=0;
        while(Time.realtimeSinceStartup<deadline && map!=null && MyPlayer.Instance!=null)
        {
            distance=Vector3.Distance(start,MyPlayer.Instance.transform.position);
            var current=(Color32[])field.GetValue(map); increased=0;
            for(int i=0;i<current.Length;i++) if(current[i].r>initial[i].r) increased++;
            if(distance>100f && (inputMode || increased>0)) break;
            yield return new WaitForSecondsRealtime(1f);
        }
        if(inputMode && distance>100f)
        {
            Debug.Log($"Full map travel input READY: distance={distance:F1}; waiting for map button");
            deadline=Time.realtimeSinceStartup+45f;
            while(map!=null && !map.mapDisplayTransform.gameObject.activeInHierarchy && Time.realtimeSinceStartup<deadline) yield return null;
            if(map==null) { Debug.LogError("Full map travel input INCONCLUSIVE: map destroyed"); yield break; }
            bool opened=map!=null && map.mapDisplayTransform.gameObject.activeInHierarchy;
            if(opened) yield return new WaitForSecondsRealtime(1f);
            var pixels=(Color32[])field.GetValue(map); increased=0;
            for(int i=0;i<pixels.Length;i++) if(pixels[i].r>initial[i].r) increased++;
            Debug.Log($"Full map travel input OPEN: opened={opened}, increasedPixels={increased}");
            deadline=Time.realtimeSinceStartup+20f;
            while(map!=null && map.mapDisplayTransform.gameObject.activeInHierarchy && Time.realtimeSinceStartup<deadline) yield return null;
            bool closed=map!=null && !map.mapDisplayTransform.gameObject.activeInHierarchy;
            Debug.Log($"Full map travel input {(opened && closed && increased>0 ? "PASS" : "FAIL")}: distance={distance:F1}, increasedPixels={increased}, closed={closed}");
            Destroy(gameObject); yield break;
        }
        Debug.Log($"Full map travel {(distance>100f && increased>0 ? "PASS" : "INCONCLUSIVE")}: distance={distance:F1}, increasedPixels={increased}, start={start}, end={(MyPlayer.Instance!=null ? MyPlayer.Instance.transform.position : Vector3.zero)}");
        if(!inputMode) FullMapUi.A_Toggle?.Invoke(false);
        Destroy(gameObject);
    }
}
#endif
