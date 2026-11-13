#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Observes real menu selection; it never changes focus or scroll position.
internal sealed class AndroidAutoScrollProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_auto_scroll_probe", false)) return;
        var go = new GameObject("Auto scroll probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidAutoScrollProbe>();
    }
    IEnumerator Start()
    {
        GameObject previous = null;
        while (true)
        {
            yield return new WaitForEndOfFrame();
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == previous) continue;
            previous = selected;
            if (selected == null) continue;
            foreach (var scroll in FindObjectsOfType<AutoScrollRect>())
            {
                if (scroll.content == null || scroll.viewport == null || !selected.transform.IsChildOf(scroll.content)) continue;
                var rect = selected.GetComponent<RectTransform>(); if (rect == null) continue;
                var view = new Vector3[4]; var item = new Vector3[4];
                scroll.viewport.GetWorldCorners(view); rect.GetWorldCorners(item);
                float top = scroll.viewport.InverseTransformPoint(item[1]).y;
                float bottom = scroll.viewport.InverseTransformPoint(item[0]).y;
                float viewTop = scroll.viewport.InverseTransformPoint(view[1]).y;
                float viewBottom = scroll.viewport.InverseTransformPoint(view[0]).y;
                Debug.Log($"Auto scroll probe: selected={selected.name}, contentY={scroll.content.anchoredPosition.y:F2}, item={bottom:F2}..{top:F2}, viewport={viewBottom:F2}..{viewTop:F2}, tracked={scroll.lastSelected == selected}");
            }
        }
    }
}
#endif
