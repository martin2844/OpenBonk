using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Camera;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Utility;
using MilkShake;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CameraShakePlayCheck
{
    const string Pending = "CameraShakePlayCheck.pending";
    static CameraShakePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("camera shake test runner").AddComponent<CameraShakePlayRunner>().Begin(); } }
}
public class CameraShakePlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("CameraShakePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var root = new GameObject("camera shake"); root.SetActive(false);
        var shaker = root.AddComponent<Shaker>(); var control = root.AddComponent<CameraShakerController>(); control.shaker = shaker;
        control.damageShake = AssetDatabase.LoadAssetAtPath<ShakePreset>("Assets/MonoBehaviour/DamageShake.asset");
        control.grindShake = AssetDatabase.LoadAssetAtPath<ShakePreset>("Assets/MonoBehaviour/GrindShake.asset");
        control.objectImpactShake = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ShakePreset>("Assets/MonoBehaviour/ObjectImpact.asset"));
        control.pylonSpawnShake = AssetDatabase.LoadAssetAtPath<ShakePreset>("Assets/MonoBehaviour/PylonsSpawn.asset");
        root.SetActive(true); yield return null;
        try
        {
            var list = (List<ShakeInstance>)typeof(Shaker).GetField("activeShakes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shaker);
            var dc = new DamageContainer(1f, "test") { damage = 1f }; MyTime.time = 5f;
            PlayerHealth.A_TakeDamage?.Invoke(null, dc, false); Check(list.Count == 0, "small damage ignored");
            dc.damage = 10f; dc.damageSource = ItemKevin.damageSource; PlayerHealth.A_TakeDamage?.Invoke(null, dc, false); Check(list.Count == 0, "Kevin damage ignored");
            dc.damageSource = "test"; PlayerHealth.A_TakeDamage?.Invoke(null, dc, false); PlayerHealth.A_TakeDamage?.Invoke(null, dc, false); Check(list.Count == 1, "damage and cooldown");
            CurrentSettings.A_SettingUpdated?.Invoke("camera_shake", 1, 0); Check(!shaker.enabled, "setting disables camera shake");
            CurrentSettings.A_SettingUpdated?.Invoke("camera_shake", 0, 1); Check(shaker.enabled, "setting enables camera shake");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null; yield return null; yield return null;
        try
        {
            Check(root.transform.localPosition.sqrMagnitude > 0f, "damage produces camera movement");
            var list = (List<ShakeInstance>)typeof(Shaker).GetField("activeShakes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shaker);
            PlayerMovement.A_ToggleGrind?.Invoke(true); Check(list.Count >= 2, "grind starts sustained shake");
            var grind = list[list.Count - 1]; PlayerMovement.A_ToggleGrind?.Invoke(false); Check(grind.State == ShakeState.FadingOut && grind.RemoveWhenStopped, "grind stops");
            SmokeAndShakeObject.A_Impact?.Invoke(.7f); Check(Mathf.Abs(control.objectImpactShake.Strength - 1.4f) < .001f, "impact scales strength");
            int count = list.Count; FinalFightController.A_PylonsStarted?.Invoke(); Check(list.Count == count + 1, "pylon event adds shake");
            Check(shaker.ShakeFromPoint(root.transform.position + Vector3.right * 10f, 10f, control.damageShake, 4) == null, "distance boundary excluded");
            var near = shaker.ShakeFromPoint(root.transform.position + Vector3.right * 5f, 10f, control.damageShake, 4);
            Check(near != null && Mathf.Abs(near.StrengthScale - .5f) < .001f && Mathf.Abs(near.RoughnessScale - .5f) < .001f, "distance scales shake");
            Shaker.GlobalShakers.Add(shaker); root.SetActive(false); count = list.Count; Shaker.ShakeAll(control.damageShake, 5); Check(list.Count == count, "global shake skips inactive object"); Shaker.GlobalShakers.Remove(shaker); root.SetActive(true);
            DestroyImmediate(root); count = list.Count; FinalFightController.A_PylonsStarted?.Invoke(); Check(list.Count == count, "cleanup unsubscribes");
            Debug.Log("CameraShakePlayCheck passed: damage, motion, settings, grind, impact, pylon, distance and cleanup"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
