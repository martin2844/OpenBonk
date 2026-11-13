#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable run: force Graveyard to observe actual procedural tiles and physics.
internal sealed class AndroidTiledMapProbe : MonoBehaviour
{
    static bool enabledForRun;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_tiled_map_probe", false)) return;
        enabledForRun = true;
        var obj = new GameObject("tiled map probe"); DontDestroyOnLoad(obj); obj.AddComponent<AndroidTiledMapProbe>();
    }
    public static void Configure(RunConfig config)
    {
        if (!enabledForRun) return;
        MapData map = DataManager.Instance != null ? DataManager.Instance.GetMap(EMap.Graveyard) : null;
        if (map == null || map.stages == null || map.stages.Length == 0) { Debug.LogError("Tiled map probe FAIL: Graveyard unavailable"); return; }
        config.mapData = map; config.stageData = map.stages[0]; config.mapTierIndex = 0;
        Debug.Log($"Tiled map probe configured: map={map.name}, stage={config.stageData.name}");
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 240f;
        ProceduralTileGeneration generator = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            generator = FindObjectOfType<ProceduralTileGeneration>();
            if (generator != null && generator.collider != null && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && GameManager.Instance != null && GameManager.Instance.IsPlaying()) break;
            yield return null;
        }
        if (generator == null || generator.collider == null) { Debug.LogError("Tiled map probe FAIL: generated collider unavailable"); yield break; }
        MeshCollider collider = generator.collider.GetComponent<MeshCollider>();
        if (collider == null || collider.sharedMesh == null) { Debug.LogError("Tiled map probe FAIL: collider mesh unavailable"); yield break; }
        Mesh mesh = collider.sharedMesh;
        Vector3[] vertices = mesh.vertices; int[] indices = mesh.triangles;
        int attempted = 0, hits = 0;
        for (int i = 0; i + 2 < indices.Length && attempted < 40; i += 3)
        {
            Vector3 a = collider.transform.TransformPoint(vertices[indices[i]]), b = collider.transform.TransformPoint(vertices[indices[i + 1]]), c = collider.transform.TransformPoint(vertices[indices[i + 2]]);
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (normal.sqrMagnitude < .5f) continue;
            attempted++;
            if (collider.Raycast(new Ray((a + b + c) / 3f + normal * 2f, -normal), out var hit, 4f) && hit.collider == collider) hits++;
        }
        bool playerStarted = MyPlayer.Instance != null && MyPlayer.Instance.hasStarted;
        bool pass = MapController.currentMap != null && MapController.currentMap.eMap == EMap.Graveyard && generator.tiles.Count > 0 && vertices.Length > 0 && indices.Length > 0 && hits > 0 && playerStarted;
        Debug.Log($"Tiled map probe {(pass ? "PASS" : "FAIL")}: map={MapController.currentMap?.name}, tiles={generator.tiles.Count}, vertices={vertices.Length}, indices={indices.Length}, rayHits={hits}/{attempted}, playerStarted={playerStarted}");
    }
}
#endif
