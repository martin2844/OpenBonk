#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in visual investigation: suppress the existing terrain outline temporarily.
internal sealed class AndroidTerrainSeamProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_terrain_seam_probe", false)) return;
        var root = new GameObject("terrain seam probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidTerrainSeamProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EnemyManager.Instance == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        if (MyPlayer.Instance == null || EnemyManager.Instance == null) { Debug.LogError("Terrain seam probe: player unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        var player = MyPlayer.Instance; var enemies = EnemyManager.Instance;
        bool waves = enemies.enabledWaves; enemies.enabledWaves = false;
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
        bool wasInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
        var materials = new Dictionary<Material, float>();
        var legacy = new HashSet<Material>();
        var unsupported = new HashSet<Material>();
        foreach (var renderer in FindObjectsOfType<Renderer>())
            foreach (var material in renderer.sharedMaterials)
            {
                if (material != null && material.shader != null && material.shader.name.StartsWith("Toon/Lit Tri Planar") && material.HasProperty("_OutlineWidth") && !materials.ContainsKey(material))
                    materials.Add(material, material.GetFloat("_OutlineWidth"));
                if (material != null && material.shader != null && material.shader.name == "Legacy Shaders/VertexLit" && legacy.Add(material))
                    Debug.Log($"Terrain seam probe: legacy renderer={renderer.name}, material={material.name}, instancing={material.enableInstancing}");
                if (material != null && material.shader != null && !material.shader.isSupported && unsupported.Add(material))
                    Debug.Log($"Terrain seam probe: unsupported renderer={renderer.name}, material={material.name}, shader={material.shader.name}");
            }
        Debug.Log($"Terrain seam probe: legacy unique materials={legacy.Count}");
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            if (material != null && material.shader != null && (material.shader.name == "Legacy Shaders/VertexLit" || !material.shader.isSupported))
                Debug.Log($"Terrain seam probe: loaded material={material.name}, shader={material.shader.name}, supported={material.shader.isSupported}, instancing={material.enableInstancing}");
        Debug.Log($"Terrain seam probe: baseline materials={materials.Count}, names={string.Join(",", System.Linq.Enumerable.Select(materials, p => p.Key.name + ":" + p.Value))}");
        var instancedMaterials = new List<Material>();
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            if (material != null && material.enableInstancing) { instancedMaterials.Add(material); material.enableInstancing = false; }
        Debug.Log($"Terrain seam probe: instancing disabled on {instancedMaterials.Count} loaded materials");
        yield return new WaitForSecondsRealtime(15f);
        foreach (var material in instancedMaterials) if (material != null) material.enableInstancing = true;
        Debug.Log("Terrain seam probe: instancing restored");
        instancedMaterials.Sort((a, b) => string.CompareOrdinal((a.shader != null ? a.shader.name : "") + "/" + a.name, (b.shader != null ? b.shader.name : "") + "/" + b.name));
        int quarterStart = 3 * instancedMaterials.Count / 4;
        int suspectStart = quarterStart + 3 * (instancedMaterials.Count - quarterStart) / 4;
        int suspectCount = instancedMaterials.Count - suspectStart;
        for (int half = 0; half < suspectCount; half++)
        {
            int start = suspectStart + half;
            int end = start + 1;
            for (int i = start; i < end; i++) if (instancedMaterials[i] != null) instancedMaterials[i].enableInstancing = false;
            Debug.Log($"Terrain seam probe: subset {half} disabled range={start}-{end - 1}, first={instancedMaterials[start].shader.name}/{instancedMaterials[start].name}, last={instancedMaterials[end - 1].shader.name}/{instancedMaterials[end - 1].name}");
            yield return new WaitForSecondsRealtime(4f);
            for (int i = start; i < end; i++) if (instancedMaterials[i] != null) instancedMaterials[i].enableInstancing = true;
            Debug.Log($"Terrain seam probe: subset {half} restored");
            yield return new WaitForSecondsRealtime(1f);
        }
        yield return new WaitForSecondsRealtime(12f);
        foreach (var item in materials) item.Key.SetFloat("_OutlineWidth", 0f);
        Debug.Log("Terrain seam probe: outline disabled");
        yield return new WaitForSecondsRealtime(20f);
        foreach (var item in materials) if (item.Key != null) item.Key.SetFloat("_OutlineWidth", item.Value);
        Debug.Log("Terrain seam probe: outline restored");
        body.constraints = constraints; invincible.SetValue(player, wasInvincible); enemies.enabledWaves = waves;
    }
}
#endif
