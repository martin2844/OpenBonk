#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using UnityEngine;
using Assets.Scripts.Utility;

// Diagnostic waits until the real character selection screen is open.
internal sealed class AndroidExternalVisualProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "openbonk_external_visual_probe", false)) return;
        var go = new GameObject("External visual probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalVisualProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120;
        CharacterDisplay display = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            bool menuOpen = false;
            foreach (var menu in Resources.FindObjectsOfTypeAll<CharacterMenu>()) if (menu != null && menu.isActiveAndEnabled && menu.gameObject.scene.IsValid()) menuOpen = true;
            if (menuOpen) foreach (var candidate in Resources.FindObjectsOfTypeAll<CharacterDisplay>())
                if (candidate != null && candidate.gameObject.scene.IsValid() && candidate.playerRenderer != null) { display = candidate; break; }
            if (display != null && DataManager.Instance != null && SaveManager.Instance != null && SaveManager.Instance.config != null) break;
            yield return null;
        }
        if (display == null) { Debug.LogError("External visual probe INCONCLUSIVE: selection screen not opened"); Destroy(gameObject); yield break; }
        // Allow the menu's delayed initial focus to finish selecting Fox.
        yield return new WaitForSecondsRealtime(4f);
        var actor = display.playerRenderer;
        var id = (ECharacter)OpenBonkContentIds.Character("pikachu-test", "pikachu");
        CharacterData external;
        try
        {
            external = DataManager.Instance.GetCharacterData(id);
            actor.SetCharacter(external, null, Vector3.zero); actor.SetIdle(); actor.ForceMoving(true);
            Debug.Log("External visual probe model instantiated; sampling walk");
        }
        catch (Exception e) { Debug.LogError("External visual probe FAIL setup: " + e); Destroy(gameObject); yield break; }
        yield return null;
        if (actor.renderer == null) { Debug.LogError("External visual probe FAIL: no renderer"); Destroy(gameObject); yield break; }
        var m = new Mesh(); actor.renderer.BakeMesh(m); var first = m.vertices; Destroy(m);
        yield return new WaitForSecondsRealtime(.4f);
        m = new Mesh(); actor.renderer.BakeMesh(m); var second = m.vertices; Destroy(m);
        float max = 0;
        for (int i = 0; i < Math.Min(first.Length, second.Length); i++) max = Mathf.Max(max, (first[i] - second[i]).magnitude);
        Debug.Log($"External visual probe {(max > .00001f ? "PASS" : "FAIL")}: vertices={first.Length} displacement={max:F4} paused={MyTime.paused} scale={actor.renderer.bounds.size}");
        var externalAnimator = actor.renderer.GetComponentInParent<OpenBonkExternalAnimator>();
        var timeField = typeof(OpenBonkExternalAnimator).GetField("time", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (externalAnimator != null && timeField != null && !MyTime.paused)
        {
            MyTime.Pause(); float frozenAt = (float)timeField.GetValue(externalAnimator);
            yield return new WaitForSecondsRealtime(.4f);
            float frozenAfter = (float)timeField.GetValue(externalAnimator); MyTime.Unpause();
            Debug.Log($"External animation pause {(Mathf.Abs(frozenAfter-frozenAt) < .00001f ? "PASS" : "FAIL")}: delta={frozenAfter-frozenAt:F5}");
            float previousScale = MyTime.timeScale; MyTime.SetTimeScale(.25f, 10f);
            float startTime = (float)timeField.GetValue(externalAnimator), startedAt = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(.5f);
            float elapsed = Time.realtimeSinceStartup-startedAt, endTime = (float)timeField.GetValue(externalAnimator);
            MyTime.SetTimeScale(previousScale, 0f);
            float clipLength = ((OpenBonkExternalCharacterData)external).walkAnimation.length;
            float advanced = (endTime-startTime+clipLength) % clipLength;
            float ratio = elapsed > 0f ? advanced / elapsed : 0f;
            Debug.Log($"External animation slow motion {(ratio > .1f && ratio < .4f ? "PASS" : "FAIL")}: ratio={ratio:F3}");
        }
        else Debug.LogWarning("External animation timing INCONCLUSIVE");
        actor.ForceMoving(false);
        yield return new WaitForSecondsRealtime(20);
        try
        {
            var original = DataManager.Instance.GetCharacterData(ECharacter.Fox);
            actor.SetCharacter(original, null, Vector3.zero); actor.SetIdle();
            Debug.Log($"External visual restore {(actor.animator != null && actor.animator.runtimeAnimatorController != null ? "PASS" : "FAIL")}");
        }
        catch (Exception e) { Debug.LogError("External visual restore FAIL: " + e); }
        Destroy(gameObject);
    }
}
#endif
