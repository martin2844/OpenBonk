#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in diagnostic for an isolated emulator; it never runs in a normal launch.
internal sealed class AndroidRandomObjectPlacementProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_random_placement_probe", false)) return;
        var root = new GameObject("Random placement probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidRandomObjectPlacementProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || SaveManager.Instance == null)) yield return null;
        if (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || SaveManager.Instance == null) { Debug.LogError("Random placement probe INCONCLUSIVE: game unavailable"); yield break; }
        var savedRandom = Random.state;
        var anchor = new Vector3(20000f, 0f, 20000f);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var placerObject = new GameObject("Placement diagnostic");
        try
        {
            floor.name = "placement probe floor"; floor.layer = LayerMask.NameToLayer("Ground");
            floor.transform.position = anchor + Vector3.down; floor.transform.localScale = new Vector3(30f, 2f, 30f);
            prefab.name = "placement probe prefab"; prefab.transform.position = anchor + Vector3.right * 500f;
            var placer = placerObject.AddComponent<RandomObjectPlacer>(); placer.center = anchor; placer.size = Vector3.zero;
            var config = new OpenBonkMapObjects.DecorativeObject { amount = 1, maxAmount = 0, checkRadius = .1f, scaleMin = 1f, scaleMax = 1f, maxSlopeAngle = 45f, prefabs = new[] { prefab } };
            floor.SetActive(false); Physics.SyncTransforms(); placer.Generate(new[] { config }, 1f);
            int invalidCount = CountClones();
            floor.SetActive(true); Physics.SyncTransforms(); placer.Generate(new[] { config }, 1f);
            int validCount = CountClones();
            Debug.Log($"Random placement probe {(invalidCount == 0 && validCount == 1 ? "PASS" : "FAIL")}: invalidClones={invalidCount} validClones={validCount}");
        }
        finally
        {
            foreach (var t in FindObjectsOfType<Transform>()) if (t.name == "placement probe prefab(Clone)") Destroy(t.gameObject);
            Destroy(floor); Destroy(prefab); Destroy(placerObject); Random.state = savedRandom;
        }
    }
    private static int CountClones()
    {
        int count = 0;
        foreach (var t in FindObjectsOfType<Transform>()) if (t.name == "placement probe prefab(Clone)") count++;
        return count;
    }
}
#endif
