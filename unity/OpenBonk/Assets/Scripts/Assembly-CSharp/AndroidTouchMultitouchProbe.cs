#if UNITY_ANDROID && !UNITY_EDITOR && (!OPENBONK_BETA_NO_PROBES || OPENBONK_TOUCH_QA)
using System.Collections;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

// Opt-in check that sends two independent pointer IDs through the real UI
// handlers. It does not simulate Android's touchscreen driver.
internal sealed class AndroidTouchMultitouchProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_touch_multitouch_probe", false)) return;
        var root = new GameObject("Touch multitouch probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidTouchMultitouchProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        GameObject overlay = null;
        while (Time.realtimeSinceStartup < deadline && (overlay = GameObject.Find("Touch overlay")) == null) yield return null;
        if (overlay == null || EventSystem.current == null) { Debug.LogError("Touch multitouch probe INCONCLUSIVE: overlay unavailable"); yield break; }
        var stick = overlay.GetComponentsInChildren<AndroidTouchStick>().FirstOrDefault(s => !s.look);
        var look = overlay.GetComponentsInChildren<AndroidTouchStick>().FirstOrDefault(s => s.look);
        var jump = overlay.GetComponentsInChildren<AndroidTouchButton>().FirstOrDefault(b => b.action == "Jump");
        if (stick == null || look == null || jump == null) { Debug.LogError("Touch multitouch probe INCONCLUSIVE: controls unavailable"); yield break; }

        yield return CheckJumpTiming(jump);

        var move = new PointerEventData(EventSystem.current) { pointerId = 101, position = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position) };
        var press = new PointerEventData(EventSystem.current) { pointerId = 102, position = RectTransformUtility.WorldToScreenPoint(null, jump.transform.position) };
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerDownHandler);
        move.position += Vector2.up * 90f;
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSecondsRealtime(1f);
        float simultaneousMove = MyInputManager.GetAxis(MyInputManager.MoveVertical);
        bool simultaneousJump = MyInputManager.GetButton(MyInputManager.Jump);
        ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerUpHandler);
        yield return new WaitForSecondsRealtime(.6f);
        bool releasedJump = !MyInputManager.GetButton(MyInputManager.Jump);
        float remainingMove = MyInputManager.GetAxis(MyInputManager.MoveVertical);
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerUpHandler);
        yield return new WaitForSecondsRealtime(.6f);
        float releasedMove = MyInputManager.GetAxis(MyInputManager.MoveVertical);
        bool passed = simultaneousMove > .4f && simultaneousJump && releasedJump && remainingMove > .4f && Mathf.Abs(releasedMove) < .01f;
        Debug.Log($"Touch multitouch probe {(passed ? "PASS" : "FAIL")}: bothMove={simultaneousMove:F2} bothJump={simultaneousJump} jumpReleased={releasedJump} remainingMove={remainingMove:F2} releasedMove={releasedMove:F2}");

        move.pointerId = 301;
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerDownHandler);
        move.position += Vector2.up * 40f;
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.dragHandler);
        bool moveOnly = AndroidTouchInput.Move.magnitude > .4f && AndroidTouchInput.Look == Vector2.zero;
        var camera = new PointerEventData(EventSystem.current) { pointerId = 302, position = RectTransformUtility.WorldToScreenPoint(null, look.transform.position) };
        ExecuteEvents.Execute(look.gameObject, camera, ExecuteEvents.pointerDownHandler);
        camera.position += Vector2.right * 90f;
        ExecuteEvents.Execute(look.gameObject, camera, ExecuteEvents.dragHandler);
        bool bothSticks = AndroidTouchInput.Move.magnitude > .4f && AndroidTouchInput.Look.x > .4f;
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerUpHandler);
        bool lookHeld = AndroidTouchInput.Move == Vector2.zero && AndroidTouchInput.Look.x > .4f;
        ExecuteEvents.Execute(look.gameObject, camera, ExecuteEvents.pointerUpHandler);
        bool bothReleased = AndroidTouchInput.Look == Vector2.zero;
        Debug.Log($"Touch dual-stick probe {(moveOnly && bothSticks && lookHeld && bothReleased ? "PASS" : "FAIL")}: moveOnly={moveOnly} both={bothSticks} lookHeld={lookHeld} released={bothReleased}");

        CheckActionLook(overlay);

        // Android can interrupt a gesture without delivering pointer-up.
        // Exercise focus loss and make sure a new pointer can use the stick.
        move.pointerId = 201;
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerDownHandler);
        var controls = overlay.GetComponentInParent<AndroidTouchControls>();
        controls.SendMessage("OnApplicationFocus", false);
        bool cleared = !overlay.activeSelf && AndroidTouchInput.Move == Vector2.zero;
        controls.SendMessage("OnApplicationFocus", true);
        yield return null;
        yield return null;
        move.pointerId = 202;
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSecondsRealtime(.5f);
        float afterFocus = MyInputManager.GetAxis(MyInputManager.MoveVertical);
        ExecuteEvents.Execute(stick.gameObject, move, ExecuteEvents.pointerUpHandler);
        bool focusPassed = cleared && overlay.activeInHierarchy && afterFocus > .4f;
        Debug.Log($"Touch focus probe {(focusPassed ? "PASS" : "FAIL")}: cleared={cleared} visible={overlay.activeInHierarchy} recapturedMove={afterFocus:F2}");
    }

    private IEnumerator CheckJumpTiming(AndroidTouchButton jump)
    {
        var player = MyPlayer.Instance;
        var movement = player != null ? player.playerMovement : null;
        if (movement == null) { Debug.LogError("Touch jump timing probe INCONCLUSIVE: no player"); yield break; }
        int count = 0;
        float jumpedAt = -1f;
        void Jumped(PlayerMovement who) { if (who == movement) { count++; jumpedAt = Time.fixedTime; } }
        PlayerMovement.A_Jumped += Jumped;
        var press = new PointerEventData(EventSystem.current) { pointerId = 701, position = RectTransformUtility.WorldToScreenPoint(null, jump.transform.position) };
        try
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline &&
                (!movement.grounded || !movement.CanBhopJump() || MyTime.paused || MyTime.unpauseTick >= MyTime.tick ||
                 PlayerCamera.Instance == null || PlayerCamera.Instance.GetCameraState() != PlayerCamera.ECameraState.Player3rd))
                yield return null;
            if (!movement.grounded || !movement.CanBhopJump() || MyTime.paused)
            { Debug.LogError("Touch jump timing probe INCONCLUSIVE: no eligible grounded tick"); yield break; }
            float pressedAt = Time.fixedTime;
            ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerDownHandler);
            yield return new WaitForFixedUpdate();
            bool firstTick = count == 1 && jumpedAt - pressedAt <= Time.fixedDeltaTime * 1.1f && AndroidTouchInput.Held("Jump");
            float holdDelay = jumpedAt < 0 ? -1f : jumpedAt - pressedAt;
            // Keep the finger down through the landing: this must never add
            // another ground jump merely because the first press was held.
            deadline = Time.realtimeSinceStartup + 8f;
            yield return new WaitForSecondsRealtime(.35f);
            while (!movement.grounded && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            bool landedHeld = movement.grounded && count == 1 && AndroidTouchInput.Held("Jump");
            ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerUpHandler);
            deadline = Time.realtimeSinceStartup + 5f;
            while (!movement.CanBhopJump() && Time.realtimeSinceStartup < deadline) yield return null;
            int beforeTap = count;
            pressedAt = Time.fixedTime;
            ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerUpHandler);
            yield return new WaitForFixedUpdate();
            bool tapTick = count == beforeTap + 1 && jumpedAt - pressedAt <= Time.fixedDeltaTime * 1.1f && !AndroidTouchInput.Held("Jump");
            float tapDelay = jumpedAt - pressedAt;
            Debug.Log($"Touch jump timing probe {(firstTick && landedHeld && tapTick ? "PASS" : "FAIL")}: heldFirstTick={firstTick} holdPhysicsDelay={holdDelay:F4}s landedStillHeldWithoutRepeat={landedHeld} tapFirstTick={tapTick} tapPhysicsDelay={tapDelay:F4}s fixedStep={Time.fixedDeltaTime:F4}s; pointer-handler test, not touchscreen-driver latency");
        }
        finally
        {
            PlayerMovement.A_Jumped -= Jumped;
            ExecuteEvents.Execute(jump.gameObject, press, ExecuteEvents.pointerUpHandler);
            AndroidTouchInput.ReleaseAll();
        }
    }

    private static void CheckActionLook(GameObject overlay)
    {
        int pointer = 801;
        foreach (string action in new[] { "Jump", "Aim", "Slide" })
        {
            var button = overlay.GetComponentsInChildren<AndroidTouchButton>().FirstOrDefault(b => b.action == action);
            if (button == null) { Debug.LogError("Touch action-look probe FAIL: missing " + action); continue; }
            var press = new PointerEventData(EventSystem.current) { pointerId = pointer++, position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
            ExecuteEvents.Execute(button.gameObject, press, ExecuteEvents.pointerDownHandler);
            var motion = new Vector2(17, -11);
            var gestures = AndroidTouchInput.LookGestures;
            gestures.BeginSample(); gestures.Observe(press.pointerId, TouchPhase.Moved, motion);
            bool heldDrag = gestures.EndSample() == motion && AndroidTouchInput.Held(action);
            ExecuteEvents.Execute(button.gameObject, press, ExecuteEvents.pointerUpHandler);
            gestures.BeginSample(); gestures.Observe(press.pointerId, TouchPhase.Moved, motion);
            bool released = gestures.EndSample() == Vector2.zero && !AndroidTouchInput.Held(action);
            AndroidTouchInput.ReleaseAll();
            Debug.Log($"Touch action-look probe {(heldDrag && released ? "PASS" : "FAIL")}: action={action} heldDrag={heldDrag} released={released}; real UI handlers with supplied touch motion");
        }
    }
}
#endif
