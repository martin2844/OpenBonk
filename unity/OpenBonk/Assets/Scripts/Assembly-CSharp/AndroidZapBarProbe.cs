#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using UnityEngine;

// Opt-in observation of the real selected character and real HUD; no charge or movement is injected here.
internal sealed class AndroidZapBarProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_zapbar_probe", false)) return;
        var root = new GameObject("Zap bar probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidZapBarProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.passiveAbility == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("Zap bar probe: player unavailable"); yield break; }
        var player = MyPlayer.Instance; var ability = player.inventory.passiveAbility as PassiveAbilityZooma;
        var bar = FindObjectOfType<ZapBar>();
        if (ability == null || bar == null) { Debug.LogError($"Zap bar probe: missing ability={ability == null}, bar={bar == null}, character={player.character}"); yield break; }
        foreach (var element in new[] { bar.zapIcon, bar.zapBar })
        {
            var rect = element != null ? element.GetComponent<RectTransform>() : null;
            var canvas = element != null ? element.GetComponentInParent<Canvas>() : null;
            if (rect == null || canvas == null) { Debug.LogWarning("Zap bar visual probe: missing rect or canvas"); continue; }
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            Debug.Log($"Zap bar visual probe: object={element.name} active={element.activeInHierarchy} mode={canvas.renderMode} camera={(camera != null ? camera.name : "null")} min=({a.x:F1},{a.y:F1}) max=({b.x:F1},{b.y:F1}) screen=({Screen.width},{Screen.height})");
        }
        for (int i = 0; i < 40; i++)
        {
            Debug.Log($"Zap bar probe: character={player.character}, charge={ability.GetProgress():F4}, fill={(bar.barFill != null ? bar.barFill.localScale.x : -1f):F4}, icon={(bar.zapIcon != null && bar.zapIcon.activeInHierarchy)}, bar={(bar.zapBar != null && bar.zapBar.activeInHierarchy)}, pos={player.transform.position}");
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
#endif
