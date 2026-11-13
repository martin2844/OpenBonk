#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Opt-in observer for the real map menu; it sends no input and changes no selection.
internal sealed class AndroidMapNavigationProbe : MonoBehaviour
{
    private GameObject lastFocus;
    private MapData lastMap;
    private bool described;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_map_navigation_probe", false)) return;
        var go = new GameObject("Map navigation probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidMapNavigationProbe>();
    }

    private void Update()
    {
        var menu = FindObjectOfType<MapSelectionUi>();
        if (menu == null || !menu.gameObject.activeInHierarchy || menu.mapSelectionGroup == null) return;
        if (!described)
        {
            var entries = menu.mapSelectionGroup.GetComponentsInChildren<MapEntry>(true);
            if (entries.Length < 3 || menu.runConfig == null) return;
            foreach (var entry in entries)
            {
                var button = entry.GetComponent<Button>();
                Debug.Log($"Map navigation probe: entry={entry.mapData?.eMap} active={entry.gameObject.activeInHierarchy} interactable={(button != null && button.IsInteractable())} canSelect={entry.buttonScript?.canSelect} up={button?.navigation.selectOnUp?.name} down={button?.navigation.selectOnDown?.name}");
            }
            described = true;
        }
        var focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var currentMap = menu.runConfig != null ? menu.runConfig.mapData : null;
        if (focus == lastFocus && currentMap == lastMap) return;
        lastFocus = focus;
        lastMap = currentMap;
        var focusedEntry = focus != null ? focus.GetComponent<MapEntry>() : null;
        Debug.Log($"Map navigation probe: focus={(focus != null ? focus.name : "<none>")} focusMap={focusedEntry?.mapData?.eMap.ToString() ?? "<none>"} selectedMap={currentMap?.eMap.ToString() ?? "<none>"}");
    }
}
#endif
