#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class AndroidPauseTooltipCloseProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_pause_tooltip_close_probe",false)) return;
        var root=new GameObject("pause tooltip close probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPauseTooltipCloseProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+900f; PauseUi pause=null;
        while(Time.realtimeSinceStartup<deadline)
        {
            pause=UiManager.Instance!=null ? UiManager.Instance.pause : null;
            if(pause!=null && pause.CanPause() && MyPlayer.Instance!=null && MyPlayer.Instance.hasStarted && EventSystem.current!=null && ToolTip.Instance!=null) break;
            yield return null;
        }
        if(pause==null || !pause.CanPause()) { Debug.LogError("Pause tooltip close probe: gameplay unavailable"); yield break; }
        pause.Pause(); yield return null;
        InventoryItemPrefabUI slot=null;
        foreach(var candidate in pause.inventory.GetComponentsInChildren<InventoryItemPrefabUI>(true))
        {
            var button=candidate.GetComponent<Button>();
            if(candidate.gameObject.activeInHierarchy && button!=null && button.interactable && candidate.toolTipObject!=null && !string.IsNullOrEmpty(candidate.toolTipObject.text)) { slot=candidate; break; }
        }
        if(slot==null) { Debug.LogError("Pause tooltip close probe: selectable slot unavailable"); pause.Resume(); yield break; }
        EventSystem.current.SetSelectedGameObject(slot.gameObject); yield return null;
        var tip=ToolTip.Instance; var visible=typeof(ToolTip).GetField("visible",BindingFlags.Instance|BindingFlags.NonPublic);
        bool opened=(bool)visible.GetValue(tip); string text=tip.t_tip.text;
        pause.Resume(); bool immediate=(bool)visible.GetValue(tip); yield return null;
        bool nextFrame=(bool)visible.GetValue(tip); yield return new WaitForSecondsRealtime(.5f);
        bool later=(bool)visible.GetValue(tip);
        Debug.Log($"Pause tooltip close probe {(opened && !later?"PASS":"FAIL")}: opened={opened}, immediate={immediate}, nextFrame={nextFrame}, later={later}, alpha={tip.group.alpha:F2}, text={text}");
    }
}
#endif
