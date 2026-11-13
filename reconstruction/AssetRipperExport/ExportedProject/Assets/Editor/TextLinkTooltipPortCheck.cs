using System;
using System.Reflection;
using Assets.Scripts.UI.Mouse;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TextLinkTooltipPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Call(object obj, string name, params object[] args) { typeof(ToolTipsText).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj,args); }
    static Vector3 Center(TMP_Text text, int n)
    {
        var link = text.textInfo.linkInfo[n];
        var first = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
        var last = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex + link.linkTextLength - 1];
        return RectTransformUtility.WorldToScreenPoint(null, text.rectTransform.TransformPoint((first.bottomLeft + last.topRight) * .5f));
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var canvasObject = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var textObject = new GameObject("description", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ToolTipsText));
        textObject.transform.SetParent(canvasObject.transform, false);
        var text = textObject.GetComponent<TextMeshProUGUI>(); text.rectTransform.sizeDelta = new Vector2(600,120); text.fontSize=30;
        text.text = "<link=\"__FIRST__\">Alpha</link>  <link=\"__SECOND__\">Bravo</link>";
        var tooltipObject = new GameObject("tooltip receiver",typeof(ToolTipMouse));
        var display = new GameObject("tooltip",typeof(RectTransform)); display.transform.SetParent(canvasObject.transform,false);
        var labelObject = new GameObject("label",typeof(RectTransform),typeof(TextMeshProUGUI)); labelObject.transform.SetParent(display.transform,false);
        var receiver = tooltipObject.GetComponent<ToolTipMouse>(); receiver.parent=display; receiver.t_tooltip=labelObject.GetComponent<TextMeshProUGUI>();
        var detector = textObject.GetComponent<ToolTipsText>();
        var receiverAwake=typeof(ToolTipMouse).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic);
        var receiverDestroy=typeof(ToolTipMouse).GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic);
        int opens=0,closes=0; string lastOpen=null,lastClose=null;
        Action<string,Vector2> opened=(id,pos)=>{ opens++; lastOpen=id; };
        Action<string> closed=id=>{ closes++; lastClose=id; };
        try
        {
            receiverAwake.Invoke(receiver,null); Call(detector,"Awake");
            Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(); Require(text.textInfo.linkCount==2,"two TMP links");
            ToolTipsText.A_OpenTooltip += opened; ToolTipsText.A_CloseTooltip += closed;
            Vector3 first=Center(text,0), second=Center(text,1);
            Call(detector,"CheckForLink",first);
            Require(opens==1 && lastOpen=="__FIRST__" && display.activeSelf,"first link opens tooltip");
            Require(receiver.t_tooltip.text=="???: __FIRST__","tooltip resolves link ID");
            Call(detector,"CheckForLink",first); Require(opens==1 && closes==0,"stationary pointer does not repeat");
            Call(detector,"CheckForLink",first + new Vector3(1000,1000,0));
            Require(closes==1 && lastClose=="__FIRST__","leaving the text starts closing the tooltip");
            Call(detector,"CheckForLink",first);
            Require(opens==2 && lastOpen=="__FIRST__" && display.activeSelf,"returning to the same point reopens the tooltip");
            Call(detector,"CheckForLink",second);
            Require(opens==3 && closes==2 && lastOpen=="__SECOND__" && lastClose=="__FIRST__","switch closes old and opens new link");
            Call(detector,"OnDisable"); Require(closes==3 && lastClose=="__SECOND__","disable closes active link");
            Debug.Log("TextLinkTooltipPortCheck passed: real TMP links, re-entry, popup lookup, switching and disable cleanup");
        }
        finally { ToolTipsText.A_OpenTooltip-=opened; ToolTipsText.A_CloseTooltip-=closed; receiverDestroy.Invoke(receiver,null); UnityEngine.Object.DestroyImmediate(tooltipObject); UnityEngine.Object.DestroyImmediate(canvasObject); }
    }
}
