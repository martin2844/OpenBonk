using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Camera;
using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public static class MinimapUiPortCheck
{
    static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool b,string m) { if(!b) throw new Exception("MinimapUiPortCheck: "+m); }
    static void RunMethod(object o,string name) => o.GetType().GetMethod(name,Flags).Invoke(o,null);
    static void Near(float a,float b,string m) => Check(Mathf.Abs(a-b)<.001f,m+" got "+a);
    public static void Run()
    {
        var root=new GameObject("minimap fixture",typeof(RectTransform)); var ui=root.AddComponent<MinimapUi>(); var border=new GameObject("border").transform; border.SetParent(root.transform); ui.border=border;
        var letter=new GameObject("north").transform; letter.SetParent(border); ui.directionLetters=new[]{letter}; var imageObject=new GameObject("jammer",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)); imageObject.transform.SetParent(root.transform); ui.jammer=imageObject.GetComponent<RawImage>(); ui.jammer.color=new Color(.4f,.5f,.6f,.8f); ui.jammer.enabled=false;
        var storm=typeof(MapEventsDesert).GetField("isActiveStorm",Flags); var oldStorm=(bool)storm.GetValue(null); var modifiers=(HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames",Flags).GetValue(null); bool hadBlind=modifiers.Contains("blind"); var oldDt=MyTime.deltaTime;
        var oldRotation=MinimapCamera.A_RotationUpdated; var oldSettings=CurrentSettings.A_SettingUpdated;
        try {
            RunMethod(ui,"Awake"); MinimapCamera.A_RotationUpdated?.Invoke(45f); Near(border.eulerAngles.z,45f,"border rotation"); Near(letter.eulerAngles.z,0f,"letter remains upright");
            CurrentSettings.A_SettingUpdated?.Invoke("minimap_size",1f,1.7f); Near(root.transform.localScale.x,1.7f,"size event"); CurrentSettings.A_SettingUpdated?.Invoke("other",1f,2f); Near(root.transform.localScale.x,1.7f,"unrelated setting ignored");
            storm.SetValue(null,true); modifiers.Remove("blind"); MyTime.deltaTime=1f; ui.jammer.color=new Color(.4f,.5f,.6f,0); RunMethod(ui,"Update"); Check(ui.jammer.enabled,"storm enables jammer"); Near(ui.jammer.color.a,.24f,"storm fade in");
            storm.SetValue(null,false); modifiers.Add("blind"); RunMethod(ui,"Update"); Check(ui.jammer.enabled && ui.jammer.color.a>.24f,"blind challenge jams minimap");
            modifiers.Remove("blind"); RunMethod(ui,"Update"); Check(!ui.jammer.enabled,"clear overlay fades and disables");
            RunMethod(ui,"OnDestroy"); Check(MinimapCamera.A_RotationUpdated==oldRotation && CurrentSettings.A_SettingUpdated==oldSettings,"event cleanup");
            Debug.Log("MinimapUiPortCheck passed: compass, size settings, storm and blind jammer fade");
        } finally { storm.SetValue(null,oldStorm); if(hadBlind) modifiers.Add("blind"); else modifiers.Remove("blind"); MyTime.deltaTime=oldDt; MinimapCamera.A_RotationUpdated=oldRotation; CurrentSettings.A_SettingUpdated=oldSettings; UnityEngine.Object.DestroyImmediate(root); }
    }
}
