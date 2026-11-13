#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

// Opt-in read-only locale inventory of the built Android player.
internal sealed class AndroidLocaleOrderProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_locale_order_probe",false)) return;
        var root=new GameObject("Locale order probe");DontDestroyOnLoad(root);root.AddComponent<AndroidLocaleOrderProbe>();
    }
    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        float deadline=Time.realtimeSinceStartup+90f;
        while(Time.realtimeSinceStartup<deadline && (SaveManager.Instance==null || SaveManager.Instance.config==null || LocalizationSettings.SelectedLocale==null)) yield return null;
        var locales=LocalizationSettings.AvailableLocales?.Locales;
        if(locales==null || SaveManager.Instance==null || SaveManager.Instance.config==null) { Debug.LogError("Locale order probe INCONCLUSIVE: settings unavailable"); yield break; }
        for(int i=0;i<locales.Count;i++) Debug.Log($"Locale order probe ROW index={i} code={(locales[i]!=null?locales[i].Identifier.Code:"<NULL>")} selected={(locales[i]==LocalizationSettings.SelectedLocale)}");
        Debug.Log($"Locale order probe DONE count={locales.Count} savedIndex={SaveManager.Instance.config.cfGameSettings.language} selected={LocalizationSettings.SelectedLocale?.Identifier.Code}");
    }
}
#endif
