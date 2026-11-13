#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEngine;

// Disposable emulator probe: instantiate the map's actual Moai and let Unity call Start.
internal sealed class AndroidNoItemsSourceProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_no_items_source_probe", false)) return;
        var go = new GameObject("No items source probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidNoItemsSourceProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MapController.currentMap == null || MapController.currentMap.shrines == null)) yield return null;
        var map = MapController.currentMap;
        if (map == null || map.shrines == null || MyPlayer.Instance == null) { Debug.LogError("No items source probe: scene unavailable"); yield break; }
        var prefab = map.shrines.FirstOrDefault(p => p != null && p.GetComponentInChildren<InteractableShrineMoai>(true) != null);
        if (prefab == null) { Debug.LogError("No items source probe: map Moai prefab unavailable"); yield break; }
        var field = typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic);
        var modifiers = (HashSet<string>)field.GetValue(null);
        bool wasPresent = modifiers.Contains(ChallengesTracker.MODIFIER_NO_ITEMS); modifiers.Remove(ChallengesTracker.MODIFIER_NO_ITEMS);
        var normal = Instantiate(prefab, MyPlayer.Instance.transform.position + Vector3.forward * 20f, prefab.transform.rotation);
        yield return null; yield return null;
        bool normalActive = normal.activeSelf;
        modifiers.Add(ChallengesTracker.MODIFIER_NO_ITEMS);
        var blocked = Instantiate(prefab, MyPlayer.Instance.transform.position + Vector3.back * 20f, prefab.transform.rotation);
        yield return null; yield return null;
        bool blockedActive = blocked.activeSelf;
        Debug.Log($"No items source probe {(normalActive && !blockedActive ? "PASS" : "FAIL")}: map={map.name}, prefab={prefab.name}, normalActive={normalActive}, noItemsActive={blockedActive}, isItemSource={blocked.GetComponentInChildren<InteractableShrineMoai>(true).isItemSource}");
        if (!wasPresent) modifiers.Remove(ChallengesTracker.MODIFIER_NO_ITEMS);
        Destroy(normal); Destroy(blocked);
    }
}
#endif
