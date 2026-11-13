#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using UnityEngine;

internal sealed class AndroidMenuEntsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_menu_ents_probe", false)) return;
        var root = new GameObject("menu ents probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMenuEntsProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        MenuEnts controller = null; IList entries = null;
        var field = typeof(MenuEnts).GetField("ents", BindingFlags.NonPublic | BindingFlags.Instance);
        while (Time.realtimeSinceStartup < deadline)
        {
            controller = FindObjectOfType<MenuEnts>();
            if (controller != null) entries = field.GetValue(controller) as IList;
            if (entries != null && entries.Count > 0) break;
            yield return null;
        }
        if (entries == null || entries.Count == 0) { Debug.LogError("Menu ents probe: no spawned ents"); yield break; }
        var entry = entries[0]; var obj = (GameObject)entry.GetType().GetField("obj").GetValue(entry);
        Vector3 before = obj.transform.position;
        yield return new WaitForSecondsRealtime(2f);
        Vector3 after = obj.transform.position;
        var animator = obj.GetComponent<Animator>();
        Debug.Log($"Menu ents probe: count={entries.Count} templateActive={controller.entPrefab.activeSelf} active={obj.activeInHierarchy} moved={Vector3.Distance(before, after):F3} animator={(animator != null)} animation={(animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Walking_Menu"))}");
    }
}
#endif
