#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Utility;
using UnityEngine;
public sealed class AndroidAutoPotsProbe : MonoBehaviour
{
    private int targetId, events;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            if (!intent.Call<bool>("getBooleanExtra", "openbonk_pots_probe", false)) return;
        var obj = new GameObject("Auto pots probe"); DontDestroyOnLoad(obj); obj.AddComponent<AndroidAutoPotsProbe>();
    }
    private void OnEnable() { MyPlayer.A_PlayerInventoryInitialized += OnInventory; DetectInteractables.A_Interacted += OnInteract; }
    private void OnDisable() { MyPlayer.A_PlayerInventoryInitialized -= OnInventory; DetectInteractables.A_Interacted -= OnInteract; }
    private void OnInteract(BaseInteractable p, bool ok) { if (p != null && p.GetInstanceID() == targetId && ok) events++; }
    private void OnInventory(PlayerInventory inv) { StartCoroutine(Check()); }
    private static InteractablePot CreatePot(Vector3 pos)
    {
        var obj = new GameObject("Probe pot"); obj.transform.position = pos;
        var pot = obj.AddComponent<InteractablePot>(); pot.isSilver = true; return pot;
    }
    private IEnumerator Check()
    {
        // Inventory initialization can precede the stage's initial teleport;
        // do not time out the first pot while interaction is intentionally gated.
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline &&
            (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting ||
             GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MyTime.paused ||
             UiManager.Instance == null || UiManager.Instance.encounterWindows == null ||
             UiManager.Instance.encounterWindows.encounterInProgress)) yield return null;
        if (MyPlayer.Instance == null || MyPlayer.Instance.isTeleporting || GameManager.Instance == null ||
            !GameManager.Instance.IsPlaying() || MyTime.paused || UiManager.Instance == null ||
            UiManager.Instance.encounterWindows == null || UiManager.Instance.encounterWindows.encounterInProgress)
        { Debug.LogError("AUTOPOTS_PROBE inconclusive: run unavailable"); yield break; }
        bool startup = OpenBonkWorldPolicy.AutoOpenPots;
        var pos = MyPlayer.Instance.transform.position;
        var near = CreatePot(pos); targetId = near.GetInstanceID();
        var far = CreatePot(pos + Vector3.right * 30);
        yield return new WaitForSeconds(1f);
        bool nearOpened = near == null, farUntouched = far != null, once = events == 1;
        yield return new WaitForSeconds(1f); once &= events == 1;
        MyTime.Pause();
        var pausedPot = CreatePot(MyPlayer.Instance.transform.position);
        targetId = pausedPot.GetInstanceID(); events = 0;
        yield return new WaitForSecondsRealtime(1f);
        bool paused = pausedPot != null && events == 0;
        MyTime.Unpause();
        yield return new WaitForSeconds(1f);
        bool resumed = pausedPot == null && events == 1;
        OpenBonkWorldPolicy.SetAutoOpenPots(false);
        var off = CreatePot(MyPlayer.Instance.transform.position);
        yield return new WaitForSeconds(1f);
        bool disabled = off != null;
        bool manual = disabled && off.Interact();
        if (far != null) Destroy(far.gameObject);
        bool passed = startup && nearOpened && farUntouched && once && paused && resumed && disabled && manual;
        Debug.Log($"AUTOPOTS_PROBE {(passed ? "PASS" : "FAIL")} startup={startup} near={nearOpened} far={farUntouched} once={once} paused={paused} resumed={resumed} disabled={disabled} manual={manual} events={events}");
    }
}
#endif
