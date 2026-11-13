#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using UnityEngine;

// Opt-in controlled check of Birdo's real input and floating code.
internal sealed class AndroidFloatingInputProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_floating_input_probe", false)) return;
        var root = new GameObject("floating input probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidFloatingInputProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance;
        var ability = player != null && player.inventory != null ? player.inventory.passiveAbility as PassiveAbilityFloating : null;
        if (ability == null) { Debug.LogError("Floating input probe FAIL: player or Birdo passive missing"); yield break; }
        var movement = player.playerMovement; var body = movement.GetRb();
        var counter = typeof(PlayerMovement).GetField("resetJumpCounter", BindingFlags.Instance | BindingFlags.NonPublic);
        var jumping = typeof(PlayerMovement).GetField("jumping", BindingFlags.Instance | BindingFlags.NonPublic);
        if (body == null || counter == null || jumping == null) { Debug.LogError("Floating input probe FAIL: setup unavailable"); yield break; }
        player.enabled = false; movement.enabled = false; body.useGravity = false;
        player.transform.position += Vector3.up * 100f; Physics.SyncTransforms();
        counter.SetValue(movement, 4); jumping.SetValue(movement, false);
        body.velocity = new Vector3(3f, -8f, 4f);
        if (player.playerInput.IsHoldingJump()) { Debug.LogError("Floating input probe FAIL: jump unexpectedly held"); yield break; }
        ability.Tick();
        bool released = Mathf.Abs(body.velocity.y + 8f) < .001f;
        body.velocity = Vector3.zero;
        Debug.Log("Floating input probe READY");
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && !player.playerInput.IsHoldingJump()) yield return null;
        if (!player.playerInput.IsHoldingJump()) { Debug.LogError("Floating input probe FAIL: input timeout"); yield break; }
        player.transform.position = new Vector3(0f, 1000f, 0f); Physics.SyncTransforms();
        counter.SetValue(movement, 4); jumping.SetValue(movement, false);
        body.velocity = new Vector3(3f, -8f, 4f); bool canFloat = movement.CanFloat(); bool held = player.playerInput.IsHoldingJump(); bool ground = movement.IsTouchingGround(); int counterValue = (int)counter.GetValue(movement); bool jumpValue = (bool)jumping.GetValue(movement); Vector3 before = body.velocity; ability.Tick(); Vector3 after = body.velocity;
        bool falling = Vector3.Distance(body.velocity, new Vector3(3f, -2f, 4f)) < .001f;
        body.velocity = new Vector3(3f, 3f, 4f); ability.Tick(); bool rising = Mathf.Abs(body.velocity.y - 3f) < .001f;
        body.velocity = new Vector3(3f, -.5f, 4f); ability.Tick(); bool threshold = Mathf.Abs(body.velocity.y + .5f) < .001f;
        counter.SetValue(movement, 3); body.velocity = new Vector3(3f, -8f, 4f); ability.Tick(); bool cooldown = Mathf.Abs(body.velocity.y + 8f) < .001f;
        counter.SetValue(movement, 4); jumping.SetValue(movement, true); body.velocity = new Vector3(3f, -8f, 4f); ability.Tick(); bool jumpingGate = Mathf.Abs(body.velocity.y + 8f) < .001f;
        bool pass = released && falling && rising && threshold && cooldown && jumpingGate;
        Debug.Log($"Floating input probe {(pass ? "PASS" : "FAIL")}: held={held} canFloat={canFloat} ground={ground} counter={counterValue} jumping={jumpValue} before={before} after={after} released={released} falling={falling} rising={rising} threshold={threshold} cooldown={cooldown} jumpingGate={jumpingGate}");
    }
}
#endif
