using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BloodMageAnimationsPlayCheck
{
    const string Pending = "BloodMageAnimationsPlayCheck.pending";
    static BloodMageAnimationsPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("Blood Mage audio test").AddComponent<BloodMageAnimationsPlayRunner>().Begin();
        }
    }
}

public class BloodMageAnimationsPlayRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("BloodMageAnimationsPlayCheck: " + message); }
    static bool Close(float a, float b) => Mathf.Abs(a - b) < 0.0001f;
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BloodMage.prefab");
            Require(prefab != null, "prefab missing");
            var parent = new GameObject("inactive parent"); parent.SetActive(false);
            var obj = Instantiate(prefab, parent.transform);
            var animation = obj.GetComponentInChildren<BloodMageAnimations>(true);
            Require(animation != null && animation.audioHoverLoop != null, "prefab component or audio binding missing");
            animation.audioHoverLoop.volume = 0.5f;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var awake = typeof(BloodMageAnimations).GetMethod("Awake", flags);
            var update = typeof(BloodMageAnimations).GetMethod("Update", flags);
            awake.Invoke(animation, null);
            MyPlayer.Instance = null;
            animation.audioHoverLoop.pitch = 0.8f;
            update.Invoke(animation, null);
            Require(Close(animation.audioHoverLoop.pitch, 0.8f), "no-player guard");
            var playerObject = new GameObject("controlled player"); playerObject.SetActive(false);
            var rb = playerObject.AddComponent<Rigidbody>();
            var movement = playerObject.AddComponent<PlayerMovement>();
            typeof(PlayerMovement).GetField("rb", flags).SetValue(movement, rb);
            var player = playerObject.AddComponent<MyPlayer>(); player.playerMovement = movement; MyPlayer.Instance = player;
            playerObject.SetActive(true); movement.enabled = false; player.enabled = false; rb.isKinematic = false; rb.useGravity = false;
            MyPlayer.Instance = player;
            MyTime.deltaTime = 0.25f; MyTime.time = 0f; rb.velocity = Vector3.zero;
            update.Invoke(animation, null);
            Require(Close(animation.audioHoverLoop.pitch, 0.5f) && Close(animation.audioHoverLoop.volume, 0.15f), "standing audio");
            rb.velocity = new Vector3(30f, 0f, 40f); MyTime.time = 0.25f;
            update.Invoke(animation, null);
            Require(Close(animation.audioHoverLoop.pitch, 0.775f) && Close(animation.audioHoverLoop.volume, 0.575f), "moving audio and pulse");
            rb.velocity = new Vector3(100f, 0f, 0f); MyTime.deltaTime = 1f; MyTime.time = 0.5f;
            update.Invoke(animation, null);
            Require(Close(animation.audioHoverLoop.pitch, 1.05f) && Close(animation.audioHoverLoop.volume, 0.5f), "speed and lerp clamp");
            MyPlayer.Instance = null;
            Debug.Log("BloodMageAnimationsPlayCheck passed: original prefab, no player, idle, movement, pulse and clamping");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
