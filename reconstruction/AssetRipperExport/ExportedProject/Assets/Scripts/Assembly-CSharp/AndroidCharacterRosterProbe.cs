#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using UnityEngine;

// Opt-in visual-asset wiring audit of the actual character-selection renderer.
internal sealed class AndroidCharacterRosterProbe : MonoBehaviour
{
    string focus;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_character_roster_probe", false)) return;
        var go = new GameObject("character roster probe"); DontDestroyOnLoad(go);
        var probe = go.AddComponent<AndroidCharacterRosterProbe>();
        probe.focus = intent.Call<string>("getStringExtra", "megabonk_character_roster_focus");
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f; CharacterDisplay display = null;
        while (Time.realtimeSinceStartup < deadline) {
            display = FindObjectOfType<CharacterDisplay>();
            if (display != null && display.isActiveAndEnabled && display.playerRenderer != null && DataManager.Instance != null && SaveManager.Instance != null && SaveManager.Instance.config != null) break;
            yield return null;
        }
        if (display == null || display.playerRenderer == null) { Debug.LogError("Character roster probe: selection unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(1f);
        int checkedCount=0, failed=0, unsupported=0, missingTexture=0;
        var actor = display.playerRenderer;
        if (!string.IsNullOrEmpty(focus))
        {
            // Allow the menu's own initial selection event to finish first.
            yield return new WaitForSecondsRealtime(10f);
            foreach (var character in DataManager.Instance.unsortedCharacterData)
            {
                if (character == null || !character.isEnabled || !string.Equals(character.eCharacter.ToString(), focus, StringComparison.OrdinalIgnoreCase)) continue;
                actor.SetCharacter(character, null, Vector3.zero); actor.SetIdle();
                var skin = actor.renderer;
                Debug.Log($"Character roster focus READY: {character.eCharacter}, mesh={(skin != null && skin.sharedMesh != null ? skin.sharedMesh.name : "missing")}, renderer={(skin != null && skin.enabled)}, materials={(skin != null ? skin.sharedMaterials.Length : 0)}");
                yield return new WaitForSecondsRealtime(30f);
                yield break;
            }
            Debug.LogError("Character roster focus INCONCLUSIVE: character not found " + focus);
            yield break;
        }
        foreach (var character in DataManager.Instance.unsortedCharacterData) {
            if (character == null || !character.isEnabled) continue;
            checkedCount++;
            try {
                actor.SetCharacter(character, null, Vector3.zero); actor.SetIdle();
                var mesh = actor.renderer; var animator = actor.animator;
                if (mesh == null || mesh.sharedMesh == null || animator == null || animator.runtimeAnimatorController == null) throw new Exception("mesh or animator missing");
                var materials = mesh.sharedMaterials;
                if (materials == null || materials.Length == 0) throw new Exception("no renderer materials");
                foreach (var material in materials) {
                    if (material == null || material.shader == null || !material.shader.isSupported) unsupported++;
                    else if (material.HasProperty("_AlbedoMap") && material.GetTexture("_AlbedoMap") == null) missingTexture++;
                }
                Debug.Log($"Character roster probe OK {character.eCharacter}: mesh={mesh.sharedMesh.name} animator={animator.runtimeAnimatorController.name} materials={materials.Length}");
            } catch (Exception e) { failed++; Debug.LogError($"Character roster probe FAIL {character.eCharacter}: {e}"); }
            yield return null;
        }
        Debug.Log($"Character roster probe RESULT checked={checkedCount} failed={failed} unsupported={unsupported} missingTexture={missingTexture}");
    }
}
#endif
