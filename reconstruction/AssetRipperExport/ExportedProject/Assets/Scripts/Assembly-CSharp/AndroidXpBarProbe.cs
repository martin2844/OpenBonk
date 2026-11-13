#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Observes the real level-up window; XP is supplied separately by the optional pickup probe.
internal sealed class AndroidXpBarProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_xp_bar_probe",false)) return;
        var go=new GameObject("XP bar probe");DontDestroyOnLoad(go);go.AddComponent<AndroidXpBarProbe>();
    }
    void OnEnable(){LevelupScreen.A_LevelupEnabled+=OnShow;LevelupScreen.A_LevelUpClose+=OnClose;}
    void OnDisable(){LevelupScreen.A_LevelupEnabled-=OnShow;LevelupScreen.A_LevelUpClose-=OnClose;}
    void OnShow(){StartCoroutine(Inspect(true));}
    void OnClose(){StartCoroutine(Inspect(false));}
    IEnumerator Inspect(bool opened)
    {
        yield return null;
        var hud=FindFirstObjectByType<XpBarUI>();
        if(!hud || !hud.xpBar){Debug.LogError("XP bar probe FAIL: HUD missing");yield break;}
        bool materialIsRainbow=hud.xpBar.material==hud.rainbow;
        bool pass=opened ? materialIsRainbow && LevelupScreen.isLevelingUp : !materialIsRainbow && !LevelupScreen.isLevelingUp;
        Debug.Log($"XP bar probe {(pass?"PASS":"FAIL")}: opened={opened}, rainbow={materialIsRainbow}, leveling={LevelupScreen.isLevelingUp}, scale={hud.xpBar.transform.localScale.x:F3}, label={(hud.t_levelText ? hud.t_levelText.text : "missing")}");
    }
}
#endif
