#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Opt-in observation of a controlled temporary freeze in the real game scene.
internal sealed class AndroidPlayerStatusColorProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_status_color_probe", false)) return;
        var go = new GameObject("Status color probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPlayerStatusColorProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || player.playerRenderer == null) { Debug.LogError("Status color probe: player unavailable"); yield break; }
        var field = typeof(PlayerRenderer).GetField("activeMaterials", BindingFlags.Instance | BindingFlags.NonPublic);
        var materials = field.GetValue(player.playerRenderer) as Material[];
        if (materials == null || materials.Length == 0) { Debug.LogError("Status color probe: materials unavailable"); yield break; }
        player.inventory.statusEffects.FreezePlayer(12f);
        yield return null;
        var material = materials[0];
        Debug.Log($"Status color probe: active={material.IsKeywordEnabled("_MK_IRIDESCENCE_DEFAULT")}, color={material.GetColor("_IridescenceColor").ToString("F3")}, expected={player.playerRenderer.colorFreeze.ToString("F3")}, size={material.GetFloat("_IridescenceSize"):F2}, shader={material.shader.name}");
        yield return new WaitForSecondsRealtime(14f);
        Debug.Log($"Status color probe: after wait active={material.IsKeywordEnabled("_MK_IRIDESCENCE_DEFAULT")}, color={material.GetColor("_IridescenceColor").ToString("F3")}");
    }
}
#endif
