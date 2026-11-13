#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
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
        var jump = overlay.GetComponentsInChildren<AndroidTouchButton>().FirstOrDefault(b => b.action == "Jump");
        if (stick == null || jump == null) { Debug.LogError("Touch multitouch probe INCONCLUSIVE: controls unavailable"); yield break; }

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
}
#endif
