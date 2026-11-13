#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.IO;
using Inventory__Items__Pickups;
using UnityEngine;

// Opt-in read-only table dump in a disposable Android test package.
internal sealed class AndroidXpTableProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_xp_table_probe", false)) return;
        var root = new GameObject("XP table probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidXpTableProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline && XpUtility.xpForLevelsTable == null) yield return null;
        var table = XpUtility.xpForLevelsTable;
        if (table == null || table.Length != 10000) { Debug.LogError("XP table probe INCONCLUSIVE: table unavailable"); yield break; }
        string path = Path.Combine(Application.persistentDataPath, "xp-table-probe.bin");
        using (var file = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            foreach (int value in table) file.Write(value);
        Debug.Log($"XP table probe: wrote {table.Length} thresholds to {path}");
    }
}
#endif
