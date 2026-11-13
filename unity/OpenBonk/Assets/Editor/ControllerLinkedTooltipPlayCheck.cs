#if UNITY_ANDROID
using System;
using System.Collections;
using Assets.Scripts.UI.Mouse;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ControllerLinkedTooltipPlayCheck
{
    const string Pending="ControllerLinkedTooltipPlayCheck.pending";
    static ControllerLinkedTooltipPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
    static void State(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("tooltip runner").AddComponent<ControllerLinkedTooltipRunner>().Begin(); } }
}
public class ControllerLinkedTooltipRunner:MonoBehaviour
{
    sealed class Provider:ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable { var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_"+locale.Identifier.Code+".asset");return Addressables.ResourceManager.CreateCompletedOperation(table as T,null); } }
    static void Check(bool ok,string msg) { if(!ok) throw new Exception("ControllerLinkedTooltipPlayCheck: "+msg); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test=Run();while(true) { bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current; } }
    IEnumerator Run()
    {
        LocalizationSettings.StringDatabase.TableProvider=new Provider();LocalizationSettings.SelectedLocale=Locale.CreateLocale("es");
        var canvas=new GameObject("canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var events=new GameObject("events",typeof(EventSystem)).GetComponent<EventSystem>();
        var parent=new GameObject("picker",typeof(RectTransform),typeof(UpgradePicker));parent.transform.SetParent(canvas.transform,false);var picker=parent.GetComponent<UpgradePicker>();picker.window=parent;
        var card=new GameObject("offer",typeof(RectTransform),typeof(Image),typeof(Button),typeof(UpgradeButton));card.transform.SetParent(parent.transform,false);var offer=card.GetComponent<UpgradeButton>();offer.upgradePicker=picker;
        var desc=new GameObject("description",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(ToolTipsText));desc.transform.SetParent(card.transform,false);
        offer.t_description=desc.GetComponent<TextMeshProUGUI>();offer.t_description.rectTransform.sizeDelta=new Vector2(700,100);offer.t_description.fontSize=28;
        offer.t_description.text="<link=\"DamageMultiplier\">Daño</link> y <link=\"Armor\">Armadura</link>";
        var popup=new GameObject("popup",typeof(RectTransform));popup.transform.SetParent(canvas.transform,false);
        var label=new GameObject("label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(popup.transform,false);
        var receiverObject=new GameObject("receiver");receiverObject.SetActive(false);var receiver=receiverObject.AddComponent<ToolTipMouse>();receiver.parent=popup;receiver.t_tooltip=label.GetComponent<TextMeshProUGUI>();receiverObject.SetActive(true);
        var other=new GameObject("other",typeof(RectTransform),typeof(Image),typeof(Button),typeof(UpgradeButton));other.transform.SetParent(parent.transform,false);var otherOffer=other.GetComponent<UpgradeButton>();otherOffer.upgradePicker=picker;
        var otherText=new GameObject("other text",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(ToolTipsText));otherText.transform.SetParent(other.transform,false);otherOffer.t_description=otherText.GetComponent<TextMeshProUGUI>();otherOffer.t_description.text="sin enlaces";
        int opens=0,closes=0;Action<string,Vector2> opened=(id,pos)=>opens++;Action<string> closed=id=>closes++;ToolTipsText.A_OpenTooltip+=opened;ToolTipsText.A_CloseTooltip+=closed;
        yield return null;events.SetSelectedGameObject(card);yield return null;
        var hint=card.transform.Find("Controller tooltip hint");Check(hint!=null && hint.GetComponent<TextMeshProUGUI>().text=="R3: explicación","Spanish controller hint");
        var changed=typeof(UpgradePicker).GetMethod("OnControllerOfferTextChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        offer.t_description.text="sin enlaces";changed.Invoke(picker,new object[]{offer});yield return null;
        Check(card.transform.Find("Controller tooltip hint")==null,"changed unlinked offer removes hint");
        offer.t_description.text="<link=\"DamageMultiplier\">Daño</link> y <link=\"Armor\">Armadura</link>";changed.Invoke(picker,new object[]{offer});yield return null;
        Check(card.transform.Find("Controller tooltip hint")!=null,"changed linked offer restores hint");
        typeof(UpgradePicker).GetMethod("CycleControllerTooltip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(picker,null);yield return null;
        Check(opens==1 && receiver.t_tooltip.text=="Aumenta el daño de tus ataques.","first link and localized tooltip");
        yield return null;Check(opens==1 && closes==0,"mouse polling does not close controller tooltip");
        typeof(UpgradePicker).GetMethod("CycleControllerTooltip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(picker,null);yield return null;Check(opens==2 && closes==1 && receiver.t_tooltip.text!="Aumenta el daño de tus ataques.","cycle to second link");
        typeof(UpgradePicker).GetMethod("CycleControllerTooltip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(picker,null);yield return null;Check(opens==2 && closes==2,"cycle closes after final link");
        typeof(UpgradePicker).GetMethod("CycleControllerTooltip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(picker,null);yield return null;Check(opens==3,"cycle reopens first link");
        events.SetSelectedGameObject(other);yield return null;Check(closes==3 && other.transform.Find("Controller tooltip hint")==null,"changing selection closes and unlinked card has no hint");
        events.SetSelectedGameObject(card);yield return null;typeof(UpgradePicker).GetMethod("CycleControllerTooltip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(picker,null);yield return null;parent.SetActive(false);yield return null;Check(closes==4,"closing picker closes active tooltip");
        ToolTipsText.A_OpenTooltip-=opened;ToolTipsText.A_CloseTooltip-=closed;
        Debug.Log("ControllerLinkedTooltipPlayCheck PASSED: hint, two links, selection and close cleanup");EditorApplication.Exit(0);
    }
}
#endif
