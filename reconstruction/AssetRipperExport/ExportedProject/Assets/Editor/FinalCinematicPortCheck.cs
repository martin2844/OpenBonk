using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public static class FinalCinematicPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception("FinalCinematicPortCheck: " + message); }
    static float Seconds(object value) => (float)typeof(WaitForSeconds).GetField("m_Seconds", Private).GetValue(value);
    public static void Run()
    {
        var root = new GameObject("cinematic fixture");
        var manager = root.AddComponent<GameManager>(); var ui = root.AddComponent<UiManager>();
        ui.cinematicBars = root.AddComponent<CinematicBars>(); GameManager.Instance = manager; UiManager.Instance = ui;
        var playerCameraObject = new GameObject("player camera"); var playerCamera = playerCameraObject.AddComponent<PlayerCamera>(); PlayerCamera.Instance = playerCamera;
        var intro = root.AddComponent<FianlBossCinematic>(); intro.cameraCircling = new GameObject("intro camera").transform; intro.cameraCircling.SetParent(root.transform); intro.cameraCircling.gameObject.SetActive(false);
        intro.meteor = new GameObject("meteor"); intro.meteor.SetActive(false); intro.finalPortal = new GameObject("exit"); intro.finalPortal.SetActive(false);
        try {
            var routine = (IEnumerator)typeof(FianlBossCinematic).GetMethod("AnimateCinematic", Private).Invoke(intro, null);
            Check(routine.MoveNext() && Mathf.Abs(Seconds(routine.Current) - 1.5f) < .001f, "first delay");
            Check(manager.cutscene && !playerCameraObject.activeSelf && intro.cameraCircling.gameObject.activeSelf, "cutscene and camera switch");
            Check(routine.MoveNext() && Mathf.Abs(Seconds(routine.Current) - 4f) < .001f, "meteor delay");
            Check(intro.meteor.activeSelf, "meteor activated");
            typeof(FianlBossCinematic).GetMethod("SpawnPortal", Private).Invoke(intro, null); Check(intro.finalPortal.activeSelf, "exit portal activated");
            Debug.Log("FinalCinematicPortCheck passed: intro camera switch, meteor timing and exit activation");
        } finally {
            GameManager.Instance = null; UiManager.Instance = null; PlayerCamera.Instance = null;
            UnityEngine.Object.DestroyImmediate(intro.meteor); UnityEngine.Object.DestroyImmediate(intro.finalPortal); UnityEngine.Object.DestroyImmediate(playerCameraObject); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
