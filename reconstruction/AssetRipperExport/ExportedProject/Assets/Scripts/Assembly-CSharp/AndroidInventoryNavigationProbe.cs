#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Read-only diagnostic of actual pause inventory selection and tooltip state.
internal sealed class AndroidInventoryNavigationProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra","megabonk_inventory_navigation_probe",false)) return;
        var go=new GameObject("Inventory navigation probe");DontDestroyOnLoad(go);go.AddComponent<AndroidInventoryNavigationProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+600f;GameObject previous=null;bool wasPaused=false;
        while(Time.realtimeSinceStartup<deadline)
        {
            var pause=FindFirstObjectByType<PauseUi>(FindObjectsInactive.Include);
            bool paused=pause && pause.isActiveAndEnabled;
            var selected=EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if(paused && (!wasPaused || selected!=previous))
            {
                var slot=selected ? selected.GetComponent<InventoryItemPrefabUI>() : null;
                var tip=ToolTip.Instance;
                Debug.Log($"Inventory navigation probe: paused={paused}, selected={(selected ? selected.name : "none")}, slot={(slot != null)}, tipText={(tip && tip.t_tip ? tip.t_tip.text : "missing")}, tipAlpha={(tip && tip.group ? tip.group.alpha : -1f):F2}");
            }
            wasPaused=paused;previous=selected;yield return new WaitForSecondsRealtime(.1f);
        }
    }
}
#endif
