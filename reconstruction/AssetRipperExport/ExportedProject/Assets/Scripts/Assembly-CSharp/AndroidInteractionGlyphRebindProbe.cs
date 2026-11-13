#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in disposable emulator run, modifying only its own controller preferences.
internal sealed class AndroidInteractionGlyphRebindProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_interaction_glyph_rebind_probe", false)) return;
        var root = new GameObject("interaction glyph rebind probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidInteractionGlyphRebindProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        DetectInteractables detector = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted)
                detector = FindObjectOfType<DetectInteractables>();
            if (detector != null && detector.glyphContainer != null && detector.uiParent != null) break;
            yield return null;
        }
        if (detector == null || detector.glyphContainer == null || detector.uiParent == null) { Debug.LogError("Interaction glyph rebind probe INCONCLUSIVE: scene object unavailable"); yield break; }
        AndroidControllerBindings.Reset();
        var glyph = detector.glyphContainer;
        detector.uiParent.gameObject.SetActive(true);
        glyph.gameObject.SetActive(true);
        glyph.SetAction("Interact");
        yield return null;
        string initial = glyph.t_text.text;
        AndroidControllerBindings.Set("Interact", 0);
        yield return null;
        string changed = glyph.t_text.text;
        string jump = AndroidControllerBindings.DisplayForAction("Jump");
        glyph.gameObject.SetActive(false);
        AndroidControllerBindings.Set("Interact", 1);
        glyph.gameObject.SetActive(true);
        yield return null;
        string reenabled = glyph.t_text.text;
        AndroidControllerBindings.Reset();
        yield return null;
        string restored = glyph.t_text.text;
        bool pass = initial == "X / E" && changed == "A / E" && jump == "X" && reenabled == "B / E" && restored == "X / E";
        Debug.Log($"Interaction glyph rebind probe {(pass ? "PASS" : "FAIL")}: default={initial}, afterA={changed}, swappedJump={jump}, afterDisabledChange={reenabled}, afterReset={restored}, textEnabled={glyph.t_text.enabled}");
    }
}
#endif
