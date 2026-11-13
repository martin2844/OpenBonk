using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class OfferNavigationCombinationCheck
{
    static void Activate(Transform t) { for (; t != null; t = t.parent) t.gameObject.SetActive(true); }
    static void Require(bool value,string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        int checks=0;
        foreach (string path in new[]{"Assets/ScenesAndroid/GeneratedMap.unity", "Assets/ScenesAndroid/FinalBossMap.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var picker=Resources.FindObjectsOfTypeAll<UpgradePicker>().First(p=>p.gameObject.scene==scene);
            Activate(picker.transform); Activate(picker.tabsExplicitNavigation.content);
            var bottoms=picker.tabsExplicitNavigation.bottomButtons;
            Require(bottoms.Length==4,"expected banish, skip, refresh, leave");
            foreach(var bottom in bottoms) Activate(bottom.transform.parent);
            for(int count=1;count<=3;count++)
            for(int mask=0;mask<9;mask++)
            {
                for(int i=0;i<picker.buttons.Length;i++) if(picker.buttons[i]!=null) picker.buttons[i].gameObject.SetActive(i<count);
                for(int i=0;i<bottoms.Length;i++) bottoms[i].gameObject.SetActive(mask==8 ? i==3 : i<3 && (mask & (1<<i))!=0);
                foreach(var canvas in Resources.FindObjectsOfTypeAll<Canvas>()) if(canvas.gameObject.scene==scene && canvas.gameObject.activeInHierarchy) LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                Canvas.ForceUpdateCanvases(); picker.tabsExplicitNavigation.Refresh();
                var active=bottoms.Where(b=>b.gameObject.activeInHierarchy).OrderBy(b=>b.transform.position.x).ToArray();
                var last=picker.buttons[count-1].GetButton();
                var down=last.FindSelectableOnDown();
                Require(active.Length==0 ? down==null : active.Contains(down),$"{scene.name} count={count} mask={mask} offer down");
                for(int i=0;i<active.Length;i++)
                {
                    Require(active[i].FindSelectableOnUp()==last,$"{scene.name} count={count} mask={mask} {active[i].name} up");
                    if(i>0) Require(active[i].FindSelectableOnLeft()==active[i-1],$"{scene.name} count={count} mask={mask} {active[i].name} left");
                    if(i+1<active.Length) Require(active[i].FindSelectableOnRight()==active[i+1],$"{scene.name} count={count} mask={mask} {active[i].name} right");
                }
                checks++;
            }
        }
        Debug.Log($"OfferNavigationCombinationCheck passed: {checks} scene/count/button combinations");
    }
}
