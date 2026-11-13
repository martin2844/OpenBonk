using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Mouse;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public static class TomeLinkTooltipSceneCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_"+locale.Identifier.Code+".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool ok,string message) { if (!ok) throw new Exception(message); }
    static void Invoke(object obj,string method,params object[] args) { obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args); }
    public static void Run()
    {
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.SelectedLocale=Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider=new Provider();
        var scene=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);
        Require(scene.IsValid(),"GeneratedMap scene");
        UpgradeButton original=null; ToolTipMouse receiver=null;
        foreach(var button in Resources.FindObjectsOfTypeAll<UpgradeButton>())
            if(button.gameObject.scene==scene && button.t_description!=null && button.t_description.GetComponent<ToolTipsText>()!=null) { original=button; break; }
        foreach(var candidate in Resources.FindObjectsOfTypeAll<ToolTipMouse>())
            if(candidate.gameObject.scene==scene && candidate.parent!=null && candidate.t_tooltip!=null) { receiver=candidate; break; }
        Require(original!=null && receiver!=null,"original card and popup bindings");
        var canvasObject=new GameObject("tooltip test canvas",typeof(RectTransform),typeof(Canvas)); canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var textObject=UnityEngine.Object.Instantiate(original.t_description.gameObject,canvasObject.transform,false);
        textObject.SetActive(true); var text=textObject.GetComponent<TextMeshProUGUI>(); var detector=textObject.GetComponent<ToolTipsText>();
        text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f); text.rectTransform.anchoredPosition=Vector2.zero; text.rectTransform.sizeDelta=new Vector2(700,160);
        Invoke(receiver,"Awake"); Invoke(detector,"Awake");
        try
        {
            foreach(string code in new[]{"en","es"})
            {
                LocalizationSettings.SelectedLocale=Locale.CreateLocale(code);
                var modifier=new StatModifier { stat=EStat.DamageMultiplier, modifyType=EStatModifyType.Multiplication, modification=.08f };
                text.text=StatUtility.GetUpgradeDescriptionTomeModifier(modifier,null);
                Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(); Require(text.textInfo.linkCount==1,code+" generated tome stat link");
                var link=text.textInfo.linkInfo[0]; Require(link.GetLinkID()=="DamageMultiplier",code+" link ID");
                var first=text.textInfo.characterInfo[link.linkTextfirstCharacterIndex]; var last=text.textInfo.characterInfo[link.linkTextfirstCharacterIndex+link.linkTextLength-1];
                Vector3 screen=RectTransformUtility.WorldToScreenPoint(null,text.rectTransform.TransformPoint((first.bottomLeft+last.topRight)*.5f));
                typeof(ToolTipsText).GetField("lastPos",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(detector,screen+Vector3.left*100f);
                Invoke(detector,"CheckForLink",screen);
                var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Stats_"+code+".asset");
                string expected=table.GetEntry("DamageMultiplier_DESC").GetLocalizedString();
                Require(receiver.parent.activeSelf && receiver.t_tooltip.text==expected,code+" original popup localized stat description");
                Invoke(detector,"OnDisable");
                Debug.Log("TomeLinkTooltipSceneCheck locale="+code+" description="+text.text+" tooltip="+receiver.t_tooltip.text);
            }
            Debug.Log("TomeLinkTooltipSceneCheck passed: actual scene card, generated tome links and localized popup");
        }
        finally { Invoke(receiver,"OnDestroy"); UnityEngine.Object.DestroyImmediate(canvasObject); }
    }
}
