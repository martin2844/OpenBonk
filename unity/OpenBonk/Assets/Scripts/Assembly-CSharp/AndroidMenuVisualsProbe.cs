#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Other;
using UnityEngine;

internal sealed class AndroidMenuVisualsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_menu_visuals_probe", false)) return;
        var root = new GameObject("menu visuals probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMenuVisualsProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        MenuVisuals visuals = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            visuals = FindObjectOfType<MenuVisuals>();
            if (visuals != null && DataManager.Instance != null && DataManager.Instance.maps != null && DataManager.Instance.maps.Count > 0) break;
            yield return null;
        }
        if (visuals == null || DataManager.Instance == null) { Debug.LogError("Menu visuals probe: menu unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        foreach (var target in new[] { EMap.Forest, EMap.Desert, EMap.Graveyard })
        {
            MapData map = DataManager.Instance.maps.Find(x => x != null && x.eMap == target);
            if (map == null || map.stages == null || map.stages.Length == 0 || map.stages[0] == null) { Debug.LogError("Menu visuals probe: missing " + target); continue; }
            MapSelectionUi.A_RunConfigChanged?.Invoke(new RunConfig { mapData = map, stageData = map.stages[0] });
            yield return new WaitForSecondsRealtime(2f);
            Debug.Log($"Menu visuals probe: map={target} forest={visuals.forestBg.activeSelf} desert={visuals.desertBg.activeSelf} general={visuals.general.activeSelf} dungeon={visuals.dungeon.activeSelf} fog={RenderSettings.fogDensity:F4} terrain={visuals.terrain.materialTemplate?.name} particles={(visuals.particles != null)}");
            yield return new WaitForSecondsRealtime(3f);
        }
        Debug.Log("Menu visuals probe completed");
    }
}
#endif
