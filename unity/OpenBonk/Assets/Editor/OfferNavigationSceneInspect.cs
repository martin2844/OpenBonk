using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OfferNavigationSceneInspect
{
    static string Name(Selectable s) => s != null ? s.name : "null";
    public static void Run()
    {
        foreach (string path in new[]{"Assets/ScenesAndroid/GeneratedMap.unity", "Assets/ScenesAndroid/FinalBossMap.unity"})
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var picker in Resources.FindObjectsOfTypeAll<UpgradePicker>())
            {
                if (picker.gameObject.scene != scene) continue;
                var tabs = picker.tabsExplicitNavigation;
                Debug.Log($"OfferNavInspect scene={scene.name} picker={picker.name} tabs={(tabs != null)} content={(tabs != null && tabs.content != null ? tabs.content.name : "null")} manual={(tabs != null && tabs.manualRefresh)}");
                if (tabs == null || tabs.bottomButtons == null) continue;
                foreach (var s in tabs.bottomButtons)
                {
                    if (s == null) { Debug.Log("OfferNavInspect bottom=null"); continue; }
                    var n=s.navigation;
                    Debug.Log($"OfferNavInspect bottom={s.name} activeSelf={s.gameObject.activeSelf} up={Name(n.selectOnUp)} left={Name(n.selectOnLeft)} right={Name(n.selectOnRight)} mode={n.mode}");
                }
            }
        }
    }
}
