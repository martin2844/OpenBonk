#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts.Managers;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;

// Controlled GPU experiment, enabled only by an explicit diagnostic intent.
internal sealed class AndroidStandardMaterialExperiment : MonoBehaviour
{
    private int warnings;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_standard_material_experiment", false)) return;
        var root = new GameObject("Standard material experiment");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidStandardMaterialExperiment>();
    }
    private void OnEnable() { Application.logMessageReceived += Observe; }
    private void OnDisable() { Application.logMessageReceived -= Observe; }
    private void Observe(string message, string stack, LogType type)
    {
        if (message.Contains("GLSL link error")) warnings++;
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
            (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null))
            yield return null;
        if (PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null) { Debug.LogError("Standard material experiment: no game camera"); yield break; }
        Camera camera = PlayerCamera.Instance.camera;
        var enemyManager = EnemyManager.Instance;
        bool wavesBefore = enemyManager != null && enemyManager.enabledWaves;
        if (enemyManager != null) enemyManager.enabledWaves = false;
        var cubes = new GameObject[3];
        var renderers = new Renderer[3];
        for (int i = 0; i < cubes.Length; i++)
        {
            cubes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cubes[i].GetComponent<Collider>());
            cubes[i].transform.SetParent(camera.transform, false);
            cubes[i].transform.localPosition = new Vector3((i - 1) * 1.5f, 1f, 6f);
            cubes[i].transform.localScale = Vector3.one;
            renderers[i] = cubes[i].GetComponent<Renderer>();
        }
        foreach (string name in new[] { "Tree", "Arch", "Fence", "Fence2", "Ladder", "LightPost", "Black" })
        {
            Material original = Resources.Load<Material>("StandardFadeProbe/" + name);
            if (original == null) { Debug.LogError("Standard material experiment: missing " + name); continue; }
            foreach (bool instancing in new[] { true, false })
            {
                var material = new Material(original) { enableInstancing = instancing };
                foreach (var renderer in renderers) renderer.sharedMaterial = material;
                yield return new WaitForSecondsRealtime(1f);
                int before = warnings;
                bool visible = false;
                float until = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < until)
                {
                    foreach (var renderer in renderers) visible |= renderer.isVisible;
                    yield return null;
                }
                Debug.Log($"Standard material experiment: material={name}, instancing={instancing}, visible={visible}, linkWarnings={warnings - before}");
                foreach (var renderer in renderers) renderer.sharedMaterial = null;
                Destroy(material);
                yield return null;
            }
        }
        foreach (var cube in cubes) Destroy(cube);
        if (enemyManager != null) enemyManager.enabledWaves = wavesBefore;
        Debug.Log("Standard material experiment: finished");
    }
}
#endif
