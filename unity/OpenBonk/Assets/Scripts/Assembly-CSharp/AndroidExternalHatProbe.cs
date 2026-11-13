#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts._Data.Hats;
using UnityEngine;

// Isolated menu preview and animation study for an imported model.
internal sealed class AndroidExternalHatProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_external_hat_probe", false)) return;
        var go = new GameObject("External hat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalHatProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        CharacterDisplay display = null;
        var id = (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu");
        while (Time.realtimeSinceStartup < deadline)
        {
            if (CharacterMenu.selectedCharacter == id)
                foreach (var candidate in Resources.FindObjectsOfTypeAll<CharacterDisplay>())
                    if (candidate != null && candidate.gameObject.scene.IsValid() && candidate.isActiveAndEnabled && candidate.playerRenderer != null && candidate.playerRenderer.animator != null)
                    { display = candidate; break; }
            if (display != null) break;
            yield return null;
        }
        if (display == null) { Debug.LogError("External hat probe INCONCLUSIVE: Pikachu selection not opened"); Destroy(gameObject); yield break; }
        yield return new WaitForSecondsRealtime(4f);
        var actor = display.playerRenderer;
        var choices = new[] { EHat.Wizard, EHat.Santa, EHat.Glasses };
        bool valid = true; Transform hat = null, head = null;
        foreach (var part in actor.animator.GetComponentsInChildren<Transform>(true)) if (part.name == "Head_Armature") { head = part; break; }
        foreach (var choice in choices)
        {
            var data = DataManager.Instance.GetHat(choice); actor.SetHat(data);
            hat = null;
            foreach (var part in actor.GetComponentsInChildren<Transform>(true)) if (part.name == "PlayerHat") { hat = part; break; }
            var hatRenderer = hat != null ? hat.GetComponent<MeshRenderer>() : null;
            bool ok = hat != null && head != null && hat.gameObject.activeSelf && hat.IsChildOf(head) && hatRenderer != null && hatRenderer.sharedMaterial == data.material && hat.GetComponent<MeshFilter>().sharedMesh == data.mesh;
            if (ok)
            {
                Vector3 size = hatRenderer.bounds.size;
                ok = size.x > .05f && size.y > .05f && size.z > .05f && size.x < 4f && size.y < 4f && size.z < 4f && Vector3.Distance(hatRenderer.bounds.center, head.position) < 2f;
                Debug.Log($"External hat probe {choice} {(ok ? "PASS" : "FAIL")}: head={head.position:F3} center={hatRenderer.bounds.center:F3} size={size:F3} scale={hat.lossyScale:F3}");
            }
            else Debug.LogError($"External hat probe {choice} FAIL: attachment missing");
            valid &= ok;
            Debug.Log($"External hat probe {choice} CAPTURE READY");
            yield return new WaitForSecondsRealtime(4f);
        }
        actor.SetHat(DataManager.Instance.GetHat(EHat.Wizard));
        foreach (var part in actor.GetComponentsInChildren<Transform>(true)) if (part.name == "PlayerHat") { hat = part; break; }
        Vector3 before = hat.position; actor.ForceMoving(true);
        yield return new WaitForSecondsRealtime(.6f);
        float motion = Vector3.Distance(before, hat.position); actor.ForceMoving(false);
        bool followed = head != null && hat.IsChildOf(head) && motion > .0001f;
        actor.SetHat(null); bool hidden = !hat.gameObject.activeSelf;
        actor.SetCharacter(DataManager.Instance.GetCharacterData(ECharacter.Fox), null, Vector3.zero);
        actor.SetHat(DataManager.Instance.GetHat(EHat.Wizard));
        bool vanilla = hat.gameObject.activeSelf && actor.animator != null && actor.animator.runtimeAnimatorController != null;
        Debug.Log($"External hat probe {(valid && followed && hidden && vanilla ? "PASS" : "FAIL")}: follow={followed} motion={motion:F5} hidden={hidden} vanilla={vanilla}");
        Destroy(gameObject);
    }
}
#endif
