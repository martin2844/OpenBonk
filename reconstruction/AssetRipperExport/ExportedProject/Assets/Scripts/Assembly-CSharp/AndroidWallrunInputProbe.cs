#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using UnityEngine;

// Enabled only for isolated input testing; do not use on a player's saved data.
internal sealed class AndroidWallrunInputProbe : MonoBehaviour
{
    bool contactProbe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_wallrun_input_probe", false)) return;
        var root = new GameObject("wallrun input probe"); DontDestroyOnLoad(root); var probe = root.AddComponent<AndroidWallrunInputProbe>();
        probe.contactProbe = intent.Call<bool>("getBooleanExtra", "megabonk_wallrun_contact_probe", false);
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.playerMovement == null || MyPlayer.Instance.playerInput == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.playerMovement == null || player.playerInput == null) { Debug.LogError("Wallrun input probe INCONCLUSIVE: player unavailable"); yield break; }
        AndroidControllerBindings.Reset(); AndroidControllerBindings.Set("Wallrun", 1);
        GameObject wall = null; int climbs = 0;
        System.Action onClimb = () => climbs++;
        var originalPassive = player.inventory.passiveAbility;
        yield return new WaitForSecondsRealtime(3f);
        if (contactProbe)
        {
            player.inventory.passiveAbility = new PassiveAbilityWallClimb();
            var movement = player.playerMovement; var body = movement.GetRb();
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "controlled wallrun wall"; wall.layer = LayerMask.NameToLayer("Ground");
            wall.transform.rotation = Quaternion.Euler(0f, player.playerInput.cameraRotation.y, 0f);
            wall.transform.localScale = new Vector3(.2f, 10f, 20f);
            wall.transform.position = body.position + wall.transform.right * (movement.playerRadius + .25f) + Vector3.up * 3f;
            body.velocity = Vector3.zero; Physics.SyncTransforms();
            PlayerMovement.A_StartedWallClimb += onClimb;
            Debug.Log($"Wallrun contact setup: player={body.position} wall={wall.transform.position} radius={movement.playerRadius} yaw={player.playerInput.cameraRotation.y}");
        }
        Debug.Log($"Wallrun input probe ready: bound={MyInputManager.IsActionBound(MyInputManager.Wallrun)} input={MyInputManager.GetButton(MyInputManager.Wallrun)} movement={player.playerMovement.inputState.holdingWallrun}");
        bool pressed = false, released = false, climbed = false, ended = false, baseline = !player.playerMovement.inputState.holdingWallrun;
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && (!released || (contactProbe && !ended)))
        {
            yield return new WaitForFixedUpdate();
            bool input = MyInputManager.GetButton(MyInputManager.Wallrun), movement = player.playerMovement.inputState.holdingWallrun;
            if (input && movement && !pressed) { pressed = true; Debug.Log("Wallrun input probe: held reached player movement"); }
            if (input && movement && player.playerMovement.IsWallClimbing() && climbs > 0) climbed = true;
            if (pressed && !input && !movement) released = true;
            if (released && !player.playerMovement.IsWallClimbing()) ended = true;
        }
        Debug.Log($"Wallrun input probe {(baseline && pressed && released ? "PASS" : "FAIL")}: baseline={baseline} pressed={pressed} released={released}");
        if (contactProbe) Debug.Log($"Wallrun contact probe {(baseline && pressed && climbed && released && ended ? "PASS" : "FAIL")}: baseline={baseline} pressed={pressed} climbed={climbed} starts={climbs} released={released} ended={ended}");
        PlayerMovement.A_StartedWallClimb -= onClimb; player.inventory.passiveAbility = originalPassive; if (wall != null) Destroy(wall);
        AndroidControllerBindings.Reset(); Destroy(gameObject);
    }
}
#endif
