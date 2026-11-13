using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using Assets.Scripts.Utility.Controllers;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class InputTipPortCheck
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string m) { if(!ok) throw new Exception("InputTipPortCheck: "+m); }
    static object Field(object o,string n) => o.GetType().GetField(n,P).GetValue(o);
    public static void Run()
    {
        var oldSave=SaveManager.Instance; var oldGame=GameManager.Instance; int oldStage=Assets.Scripts.Managers.MapController.index; var oldDt=MyTime.deltaTime; var root=new GameObject("tip test",typeof(RectTransform)); root.SetActive(false); var save=root.AddComponent<SaveManager>(); save.config=new ConfigSaveFile(); save.stats=new StatsSaveFile(); save.stats.Init(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var tips=root.AddComponent<InputTipUi>(); tips.group=root.AddComponent<CanvasGroup>(); var textObj=new GameObject("tip text",typeof(RectTransform)); textObj.transform.SetParent(root.transform); tips.t_tip=textObj.AddComponent<TextMeshProUGUI>(); var glyphObj=new GameObject("glyph",typeof(RectTransform)); glyphObj.transform.SetParent(root.transform); tips.glyphContainer=glyphObj.AddComponent<MyGlyphDisplay>(); var glyphText=new GameObject("glyph text",typeof(RectTransform)); glyphText.transform.SetParent(glyphObj.transform); tips.glyphContainer.t_text=glyphText.AddComponent<TextMeshProUGUI>(); tips.glyphContainer.t_text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MonoBehaviour/alagard SDF.asset"); tips.glyphContainer.t_text.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/alagard_AlwaysOnTop.mat");
        WeaponData data=null;
        try
        {
            LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); typeof(InputTipUi).GetMethod("Awake",P).Invoke(tips,null);
            var q=(Queue<InputTip>)Field(tips,"tipQueue"); save.config.cfGameSettings.show_tips=0; tips.SetTip("test","Jump"); Check(q.Count==0,"disabled setting prevents enqueue"); save.config.cfGameSettings.show_tips=1;
            GameManager.A_RunStarted?.Invoke(); Check(q.Count==2,"first runs enqueue jump and slide"); var jump=q.Dequeue(); var slide=q.Dequeue(); Check(jump.action=="Jump" && jump.extraDelay==10f && slide.action=="Slide" && slide.extraDelay==15f && !string.IsNullOrEmpty(jump.tip),"original actions, text and delays");
            save.stats.stats["runs"].value=3; GameManager.A_RunStarted?.Invoke(); Check(q.Count==0,"experienced runs do not enqueue startup tips");
            data=ScriptableObject.CreateInstance<WeaponData>(); data.hasCrosshair=true; var weapon=(WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); weapon.weaponData=data; weapon.level=1; WeaponInventory.A_WeaponAdded?.Invoke(weapon); Check(q.Count==1 && q.Peek().action=="Aim" && q.Peek().extraDelay==1f,"first aimable weapon enqueues aim"); q.Clear(); weapon.level=2; WeaponInventory.A_WeaponAdded?.Invoke(weapon); Check(q.Count==0,"weapon upgrade does not repeat aim tip");
            var show=(IEnumerator)typeof(InputTipUi).GetMethod("ShowTip",P).Invoke(tips,new object[]{new InputTip("Jump now","Jump",0f)}); MyTime.deltaTime=.25f; Check(show.MoveNext() && tips.group.alpha==0f,"tip starts hidden"); for(int i=0;i<30 && (string)Field(tips,"currentAction")!="Jump";i++) Check(show.MoveNext(),"reaches visible state"); Check(tips.t_tip.text=="Jump now" && Mathf.Abs(tips.group.alpha-1f)<.001f,"visible tip and text");
            typeof(MyGlyphDisplay).GetMethod("ShowNativeAction",P).Invoke(tips.glyphContainer,new object[]{"Jump"}); Check(tips.glyphContainer.t_text.text.Contains("A") && tips.glyphContainer.t_text.text.Contains("Space"),"jump binding label"); typeof(MyGlyphDisplay).GetMethod("ShowNativeAction",P).Invoke(tips.glyphContainer,new object[]{"Slide"}); Check(tips.glyphContainer.t_text.text.Contains("LB"),"slide binding label"); typeof(MyGlyphDisplay).GetMethod("ShowNativeAction",P).Invoke(tips.glyphContainer,new object[]{"Aim"}); Check(tips.glyphContainer.t_text.text.Contains("LT"),"aim binding label");
            typeof(InputTipUi).GetMethod("SkipTip",P).Invoke(tips,null); for(int i=0;i<30 && show.MoveNext();i++) {} Check(!(bool)Field(tips,"isShowingTip") && tips.group.alpha==0f && (string)Field(tips,"currentAction")=="", "skip hides and resets");
            save.stats.stats["runs"].value=0; q.Clear(); Assets.Scripts.Managers.MapController.index=0; var game=root.AddComponent<GameManager>(); GameManager.Instance=game; typeof(GameManager).GetMethod("Start",P).Invoke(game,null); Check(game.HasRaisedRunStarted && q.Count==2,"run event raised for an already subscribed tip");
            var lateObj=new GameObject("late HUD tip"); lateObj.SetActive(false); var late=lateObj.AddComponent<InputTipUi>(); typeof(InputTipUi).GetMethod("Awake",P).Invoke(late,null); var lateQueue=(Queue<InputTip>)Field(late,"tipQueue"); Check(lateQueue.Count==2 && lateQueue.Peek().action=="Jump","late HUD replays missed run event once"); typeof(InputTipUi).GetMethod("OnDestroy",P).Invoke(late,null); UnityEngine.Object.DestroyImmediate(lateObj);
            Debug.Log("InputTipPortCheck passed: events, settings, first-run queue, aim trigger, timing, labels and skip");
        }
        finally { typeof(InputTipUi).GetMethod("OnDestroy",P).Invoke(tips,null); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{oldSave}); MyTime.deltaTime=oldDt; GameManager.Instance=oldGame; Assets.Scripts.Managers.MapController.index=oldStage; UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(root); }
    }
}
