#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

// Isolated emulator only. Sends UI pointer events through the touch handlers,
// then observes the real player movement and external animation driver.
internal sealed class AndroidPikachuMovementLiveProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_pikachu_movement_live_probe", false)) return;
        var root = new GameObject("Pikachu movement live probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPikachuMovementLiveProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f; GameObject overlay = null;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || (overlay = GameObject.Find("Touch overlay")) == null || EventSystem.current == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.character != (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu") || overlay == null || EnemyManager.Instance == null) { Debug.LogError("Pikachu live movement INCONCLUSIVE: Pikachu or touch overlay unavailable"); yield break; }
        var data = DataManager.Instance.GetCharacterData(player.character) as OpenBonkExternalCharacterData;
        var animator = player.playerRenderer.GetComponentInChildren<OpenBonkExternalAnimator>(true);
        var stick = overlay.GetComponentsInChildren<AndroidTouchStick>().FirstOrDefault(s => !s.look);
        var jump = overlay.GetComponentsInChildren<AndroidTouchButton>().FirstOrDefault(b => b.action == "Jump");
        var slide = overlay.GetComponentsInChildren<AndroidTouchButton>().FirstOrDefault(b => b.action == "Slide");
        if (data == null || animator == null || stick == null || jump == null || slide == null) { Debug.LogError("Pikachu live movement INCONCLUSIVE: components missing"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var w in player.inventory.weaponInventory.weapons.Values) w.Disable();
        var current = typeof(OpenBonkExternalAnimator).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic);
        var board = animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == data.surfboard);
        if (current == null || board == null) { Debug.LogError("Pikachu live movement INCONCLUSIVE: board or clip unavailable"); yield break; }
        var moveEvent = new PointerEventData(EventSystem.current) { pointerId = 101, position = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position) };
        var jumpEvent = new PointerEventData(EventSystem.current) { pointerId = 102, position = RectTransformUtility.WorldToScreenPoint(null, jump.transform.position) };
        var slideEvent = new PointerEventData(EventSystem.current) { pointerId = 103, position = RectTransformUtility.WorldToScreenPoint(null, slide.transform.position) };
        var clock = typeof(OpenBonkExternalAnimator).GetField("time", BindingFlags.Instance | BindingFlags.NonPublic);
        bool idleBefore = false, idleAfter = false, idleAnimated = false; float firstIdleTime = -1f;
        deadline = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (data.idleAnimation != null && (current.GetValue(animator) as AnimationClip) == data.idleAnimation && !board.gameObject.activeSelf)
            {
                idleBefore = true; float t = (float)clock.GetValue(animator);
                if (firstIdleTime < 0) firstIdleTime = t; else if (Mathf.Abs(t - firstIdleTime) > .1f) idleAnimated = true;
            }
            if (idleBefore && idleAnimated) break;
            yield return null;
        }
        Vector3 start = player.transform.position; bool walked = false, ran = false, jumped = false, surfed = false; float maxSpeed = 0;
        ExecuteEvents.Execute(stick.gameObject, moveEvent, ExecuteEvents.pointerDownHandler);
        moveEvent.position += Vector2.up * 15f; ExecuteEvents.Execute(stick.gameObject, moveEvent, ExecuteEvents.dragHandler);
        deadline = Time.realtimeSinceStartup + 3f;
        float walkStarted = Time.realtimeSinceStartup;
        Vector3 walkStart = player.transform.position;
        float walkDistance = 0f;
        int settledWalk = 0, settledRun = 0, settledOther = 0;
        float settledMinSpeed = float.PositiveInfinity, settledMaxSpeed = 0f;
        while (Time.realtimeSinceStartup < deadline)
        {
            walkDistance = Vector3.Distance(walkStart, player.transform.position);
            var clip = current.GetValue(animator) as AnimationClip;
            if (clip == data.walkAnimation && player.playerMovement.IsTouchingGround() && walkDistance > .2f) walked = true;
            if (Time.realtimeSinceStartup - walkStarted >= 1f && player.playerMovement.IsTouchingGround())
            {
                if (clip == data.walkAnimation) settledWalk++; else if (clip == data.runAnimation) settledRun++; else settledOther++;
                float sampleSpeed = player.playerMovement.GetVelocity().magnitude;
                settledMinSpeed = Mathf.Min(settledMinSpeed, sampleSpeed); settledMaxSpeed = Mathf.Max(settledMaxSpeed, sampleSpeed);
            }
            yield return null;
        }
        Debug.Log($"Pikachu low-stick steady samples: walk={settledWalk} run={settledRun} other={settledOther} speedMin={settledMinSpeed:F2} speedMax={settledMaxSpeed:F2} stickMagnitude={AndroidTouchInput.Move.magnitude:F3}");
        moveEvent.position += Vector2.up * 80f; ExecuteEvents.Execute(stick.gameObject, moveEvent, ExecuteEvents.dragHandler);
        deadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < deadline) { var c = current.GetValue(animator) as AnimationClip; if (!ran && c == data.runAnimation) { ran = true; ScreenCapture.CaptureScreenshot("pikachu-live-run.png"); } maxSpeed = Mathf.Max(maxSpeed, player.playerMovement.GetVelocity().magnitude); yield return null; }
        ExecuteEvents.Execute(jump.gameObject, jumpEvent, ExecuteEvents.pointerDownHandler);
        deadline = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < deadline) { if (!jumped && (current.GetValue(animator) as AnimationClip) == data.jumpAnimation && !player.playerMovement.IsTouchingGround() && !board.gameObject.activeSelf) { jumped = true; ScreenCapture.CaptureScreenshot("pikachu-live-jump.png"); } yield return null; }
        ExecuteEvents.Execute(jump.gameObject, jumpEvent, ExecuteEvents.pointerUpHandler);
        deadline = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < deadline && !player.playerMovement.IsTouchingGround()) yield return null;
        yield return new WaitForSecondsRealtime(1f);
        ExecuteEvents.Execute(slide.gameObject, slideEvent, ExecuteEvents.pointerDownHandler);
        deadline = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < deadline) { if (!surfed && (current.GetValue(animator) as AnimationClip) == data.surfAnimation && board.gameObject.activeSelf) { surfed = true;
                var boardRenderer = board.GetComponentInChildren<Renderer>();
                bool groundFound = Physics.Raycast(board.position + Vector3.up * 3f, Vector3.down, out var groundHit, 8f, player.playerMovement.whatIsGround);
                Debug.Log($"Pikachu surf visual: boardVisible={(boardRenderer != null && boardRenderer.isVisible)} boardBounds={(boardRenderer != null ? boardRenderer.bounds.ToString() : "none")} groundFound={groundFound} groundY={(groundFound ? groundHit.point.y : 0f):F3}");
                ScreenCapture.CaptureScreenshot("pikachu-live-surf.png"); } yield return null; }
        ExecuteEvents.Execute(slide.gameObject, slideEvent, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(stick.gameObject, moveEvent, ExecuteEvents.pointerUpHandler);
        deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline) { if ((current.GetValue(animator) as AnimationClip) == data.idleAnimation && data.idleAnimation != null && !board.gameObject.activeSelf) { idleAfter = true; break; } yield return null; }
        float distance = Vector3.Distance(start, player.transform.position);
        bool pass = idleBefore && idleAnimated && idleAfter && walked && ran && jumped && surfed && distance > 1f;
        Debug.Log($"Pikachu live movement {(pass ? "PASS" : "FAIL")}: idleBefore={idleBefore} idleAnimated={idleAnimated} idleAfter={idleAfter} walk={walked} walkDistance={walkDistance:F2} run={ran} jump={jumped} surf={surfed} distance={distance:F2} maxSpeed={maxSpeed:F2}");
    }
}
#endif
