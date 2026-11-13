using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class OfferNavigationCombinationInspect
{
    static string Name(Selectable s) => s != null ? s.name : "null";
    static void Activate(Transform t) { for (; t != null; t = t.parent) t.gameObject.SetActive(true); }
    public static void Run()
    {
        foreach (string path in new[]{"Assets/ScenesAndroid/GeneratedMap.unity", "Assets/ScenesAndroid/FinalBossMap.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var picker=Resources.FindObjectsOfTypeAll<UpgradePicker>().First(p=>p.gameObject.scene==scene);
            Activate(picker.transform); Activate(picker.tabsExplicitNavigation.content);
            for(int i=0;i<picker.buttons.Length;i++) if(picker.buttons[i]!=null) picker.buttons[i].gameObject.SetActive(i<3);
            var buttons=picker.tabsExplicitNavigation.bottomButtons;
            foreach(var b in buttons) if(b!=null) Activate(b.transform.parent);
            for(int mask=1;mask<8;mask++)
            {
                for(int i=0;i<buttons.Length;i++) if(buttons[i]!=null) buttons[i].gameObject.SetActive(i<3 && (mask & (1<<i))!=0);
                foreach(var canvas in Resources.FindObjectsOfTypeAll<Canvas>()) if(canvas.gameObject.scene==scene && canvas.gameObject.activeInHierarchy) LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                Canvas.ForceUpdateCanvases(); picker.tabsExplicitNavigation.Refresh();
                foreach(var b in buttons)
                {
                    if(b==null || !b.gameObject.activeInHierarchy)continue;
                    Debug.Log($"OfferNavCombination scene={scene.name} mask={mask} button={b.name} x={b.transform.position.x:F1} left={Name(b.FindSelectableOnLeft())} right={Name(b.FindSelectableOnRight())} up={Name(b.FindSelectableOnUp())}");
                }
            }
        }
    }
}
