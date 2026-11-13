#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using UnityEngine;

internal sealed class AndroidShrineHudProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_shrine_hud_probe", false)) return;
        var go = new GameObject("Shrine HUD probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidShrineHudProbe>();
    }
    private IEnumerator Start()
    {
        ShrineInformationUi hud = null;
        while (hud == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || SaveManager.Instance?.config?.cfGameSettings == null)
        {
            hud = FindFirstObjectByType<ShrineInformationUi>(FindObjectsInactive.Include); yield return null;
        }
        SaveManager.Instance.config.cfGameSettings.debug_shrines = 1;
        CurrentSettings.A_SettingUpdated?.Invoke("debug_shrines", 0, 1);
        yield return new WaitForSeconds(2f);
        var rows = hud.GetComponentsInChildren<ShrineInformationPrefab>(true).Where(r => r.gameObject.activeInHierarchy).Select(r => $"{r.t_name?.text}={r.t_values?.text}");
        var counts = InteractablesStatus.interactablesByName.Select(p => $"{p.Key}={p.Value.numUsed}/{p.Value.numTotal}");
        Debug.Log($"Shrine HUD probe: visible={hud.parent.gameObject.activeInHierarchy}; tracker=[{string.Join(", ", counts)}]; rows=[{string.Join(", ", rows)}]");
    }
}
#endif
