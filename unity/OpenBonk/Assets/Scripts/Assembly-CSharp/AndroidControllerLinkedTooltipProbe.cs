#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Mouse;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

internal sealed class AndroidControllerLinkedTooltipProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_controller_link_probe", false)) return;
        var root = new GameObject("controller linked tooltip probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidControllerLinkedTooltipProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+900f;
        while(Time.realtimeSinceStartup<deadline && (GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance==null || UiManager.Instance==null || UiManager.Instance.encounterWindows==null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        if(Time.realtimeSinceStartup>=deadline) { Debug.LogError("Controller link probe: gameplay unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("es")); yield return null;
        UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Moai); yield return new WaitForSecondsRealtime(1f);
        var screen=UiManager.Instance.encounterWindows.levelupScreen as LevelupScreen;
        if(screen==null || screen.upgradePicker==null || screen.window==null || !screen.window.activeInHierarchy) { Debug.LogError("Controller link probe: offer unavailable"); yield break; }
        UpgradeButton offer=null;foreach(var candidate in screen.upgradePicker.buttons) if(candidate!=null && candidate.gameObject.activeInHierarchy && candidate.t_description!=null) { offer=candidate; break; }
        var tome=DataManager.Instance.unsortedTomes.Find(t=>t!=null && t.eTome==ETome.Damage);
        if(offer==null || tome==null || EventSystem.current==null) { Debug.LogError("Controller link probe: missing card or tome"); yield break; }
        offer.SetUpgrade(tome);EventSystem.current.SetSelectedGameObject(null);EventSystem.current.SetSelectedGameObject(offer.gameObject);
        yield return new WaitForSecondsRealtime(2f);
        offer.t_description.ForceMeshUpdate();var hint=offer.transform.Find("Controller tooltip hint");
        Debug.Log($"Controller link probe READY: links={offer.t_description.textInfo.linkCount}, hint={(hint!=null?hint.GetComponent<TextMeshProUGUI>().text:"none")}, selected={(EventSystem.current.currentSelectedGameObject==offer.gameObject)}");
        string expected=Assets.Scripts.UI.Localization.LocalizationUtility.GetStatDesc(Assets.Scripts.Menu.Shop.EStat.DamageMultiplier);
        bool opened=false;deadline=Time.realtimeSinceStartup+120f;
        while(Time.realtimeSinceStartup<deadline)
        {
            foreach(var receiver in FindObjectsOfType<ToolTipMouse>())
                if(receiver.parent!=null && receiver.parent.activeInHierarchy && receiver.t_tooltip!=null && receiver.t_tooltip.text==expected && receiver.parent.transform.localScale.x>.1f) opened=true;
            if(opened) break;
            yield return null;
        }
        if(!opened) { Debug.LogError("Controller link probe FAIL: no tooltip after input"); yield break; }
        Debug.Log("Controller link probe OPEN: " + expected);
        yield return new WaitForSecondsRealtime(3f);
        deadline=Time.realtimeSinceStartup+45f;
        while(Time.realtimeSinceStartup<deadline)
        {
            bool any=false;foreach(var receiver in FindObjectsOfType<ToolTipMouse>()) if(receiver.parent!=null && receiver.parent.activeInHierarchy) any=true;
            if(!any) { Debug.Log("Controller link probe PASS: R3 opened and closed localized tooltip"); yield break; }
            yield return null;
        }
        Debug.LogError("Controller link probe FAIL: tooltip did not close");
    }
}
#endif
