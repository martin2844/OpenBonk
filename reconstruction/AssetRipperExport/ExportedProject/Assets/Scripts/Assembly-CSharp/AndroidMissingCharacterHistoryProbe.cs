#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Other;
using UnityEngine;
using UnityEngine.UI;

// Test package only: writes a synthetic completion to an isolated emulator save.
internal sealed class AndroidMissingCharacterHistoryProbe : MonoBehaviour
{
    private string mode;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest") return;
        string mode = intent.Call<string>("getStringExtra", "openbonk_history_probe");
        if (mode != "write" && mode != "absent" && mode != "restored") return;
        var go = new GameObject("Missing character history probe"); DontDestroyOnLoad(go);
        go.AddComponent<AndroidMissingCharacterHistoryProbe>().mode = mode;
    }
    private IEnumerator Start()
    {
        MapStatsInfoUi ui = null;
        float deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (SaveManager.loaded && SaveManager.Instance != null && SaveManager.Instance.progression != null && DataManager.Instance != null)
                foreach (var candidate in Resources.FindObjectsOfTypeAll<MapStatsInfoUi>())
                    if (candidate != null && candidate.gameObject.scene.IsValid()) { ui = candidate; break; }
            if (ui != null) break;
            yield return null;
        }
        if (ui == null) { Debug.LogError("History probe INCONCLUSIVE: menu or save unavailable"); yield break; }
        var id = (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu");
        var progress = SaveManager.Instance.progression.menuMeta.GetMapProgress(EMap.Forest);
        if (mode == "write")
        {
            if (!progress.tierCompletionsWithCharacters.TryGetValue(0, out var characters))
                progress.tierCompletionsWithCharacters[0] = characters = new HashSet<ECharacter>();
            characters.Add(id); characters.Add(ECharacter.Fox);
            SaveManager.Instance.SaveProgression();
        }
        bool registered = DataManager.Instance.TryGetCharacterData(id, out _);
        bool recorded = progress.GetTierCompletionCharacters(0).Contains(id);
        var map = DataManager.Instance.GetMap(EMap.Forest);
        ui.SetConfig(new RunConfig { mapData = map, mapTierIndex = 0 });
        var icons = (List<RawImage>)typeof(MapStatsInfoUi).GetField("characterIcons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
        int visible = 0, expected = 0;
        foreach (var icon in icons) if (icon.gameObject.activeSelf) visible++;
        foreach (var character in progress.GetTierCompletionCharacters(0))
            if (DataManager.Instance.TryGetCharacterData(character, out var data) && data != null) expected++;
        bool ok = recorded && visible == expected && expected >= 1 && registered == (mode != "absent");
        Debug.Log($"History probe {(ok ? "PASS" : "FAIL")}: mode={mode} registered={registered} recorded={recorded} visible={visible} expected={expected}");
    }
}
#endif
