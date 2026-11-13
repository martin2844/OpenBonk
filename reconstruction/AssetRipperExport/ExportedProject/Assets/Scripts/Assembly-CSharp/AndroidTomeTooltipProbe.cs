#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.UI.Mouse;
using Assets.Scripts.Menu.Shop;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Opt-in: open a controlled real offer and exercise its TMP tooltip on Android.
internal sealed class AndroidTomeTooltipProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra","megabonk_tome_tooltip_probe",false)) return;
        var root=new GameObject("tome tooltip probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidTomeTooltipProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance==null || UiManager.Instance==null || UiManager.Instance.encounterWindows==null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        if (Time.realtimeSinceStartup>=deadline) { Debug.LogError("Tome tooltip probe: gameplay unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var locale=LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("es"));
        if(locale==null) { Debug.LogError("Tome tooltip probe: Spanish unavailable"); yield break; }
        LocalizationSettings.SelectedLocale=locale; yield return null;
        UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Moai);
        yield return new WaitForSecondsRealtime(1f);
        var screen=UiManager.Instance.encounterWindows.levelupScreen as LevelupScreen;
        if(screen==null || screen.window==null || !screen.window.activeInHierarchy || screen.upgradePicker==null) { Debug.LogError("Tome tooltip probe: offer unavailable"); yield break; }
        UpgradeButton button=null;
        foreach(var candidate in screen.upgradePicker.buttons) if(candidate!=null && candidate.gameObject.activeInHierarchy && candidate.t_description!=null) { button=candidate; break; }
        var tome=DataManager.Instance.unsortedTomes.Find(t=>t!=null && t.eTome==ETome.Damage);
        if(button==null || tome==null) { Debug.LogError("Tome tooltip probe: tome or card unavailable"); yield break; }
        button.SetUpgrade(tome);
        yield return null;
        var text=button.t_description; var detector=text.GetComponent<ToolTipsText>();
        if(detector==null) { Debug.LogError("Tome tooltip probe: link detector unavailable"); yield break; }
        Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate();
        if(text.textInfo.linkCount<1) { Debug.LogError("Tome tooltip probe: no link in "+text.text); yield break; }
        var link=text.textInfo.linkInfo[0]; var first=text.textInfo.characterInfo[link.linkTextfirstCharacterIndex]; var last=text.textInfo.characterInfo[link.linkTextfirstCharacterIndex+link.linkTextLength-1];
        var canvas=text.GetComponentInParent<Canvas>(); var cam=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3 position=RectTransformUtility.WorldToScreenPoint(cam,text.rectTransform.TransformPoint((first.bottomLeft+last.topRight)*.5f));
        typeof(ToolTipsText).GetField("lastPos",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(detector,position+Vector3.left*100f);
        Debug.Log($"Tome tooltip probe: detector active={detector.isActiveAndEnabled}, openSubscribers={(ToolTipsText.A_OpenTooltip?.GetInvocationList().Length ?? 0)}, position={position}, rect={TMPro.TMP_TextUtilities.IsIntersectingRectTransform(text.rectTransform, position, cam)}, hit={TMPro.TMP_TextUtilities.FindIntersectingLink(text, position, cam)}");
        typeof(ToolTipsText).GetMethod("CheckForLink",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(detector,new object[]{position});
        foreach(var candidate in Resources.FindObjectsOfTypeAll<ToolTipMouse>())
            if(candidate.gameObject.scene.IsValid()) Debug.Log($"Tome tooltip probe: receiver={candidate.name}, active={candidate.isActiveAndEnabled}, parentActive={(candidate.parent!=null && candidate.parent.activeSelf)}, parentHierarchy={(candidate.parent!=null && candidate.parent.activeInHierarchy)}, text={(candidate.t_tooltip!=null?candidate.t_tooltip.text:"null")}");
        // The injected coordinate is not the system pointer. Suppress pointer polling briefly
        // so that the real, unrelated emulator pointer cannot immediately close the popup.
        typeof(ToolTipsText).GetField("readyTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(detector,Time.time+3f);
        yield return new WaitForSecondsRealtime(.5f);
        ToolTipMouse receiver=null;
        foreach(var candidate in FindObjectsOfType<ToolTipMouse>()) if(candidate.parent!=null && candidate.parent.activeInHierarchy && candidate.t_tooltip!=null) { receiver=candidate; break; }
        string expected=LocalizationUtility.GetStatDesc(EStat.DamageMultiplier);
        if(receiver==null || receiver.t_tooltip.text!=expected) { Debug.LogError($"Tome tooltip probe: popup mismatch expected={expected}, actual={(receiver!=null?receiver.t_tooltip.text:"none")}, link={link.GetLinkID()}, pos={position}"); yield break; }
        Debug.Log($"Tome tooltip probe: PASS description={text.text}, link={link.GetLinkID()}, tooltip={receiver.t_tooltip.text}, position={position}");
        yield return new WaitForSecondsRealtime(12f);
    }
}
#endif
