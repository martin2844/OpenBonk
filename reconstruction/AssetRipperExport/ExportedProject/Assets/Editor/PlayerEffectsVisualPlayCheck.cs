using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerEffectsVisualPlayCheck
{
    const string Pending = "PlayerEffectsVisualPlayCheck.pending";
    static PlayerEffectsVisualPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false); new GameObject("player effects runner").AddComponent<PlayerEffectsVisualPlayRunner>().Begin();
    }
}
public class PlayerEffectsVisualPlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        var playerGo = new GameObject("player"); playerGo.SetActive(false);
        var player = playerGo.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var movement = playerGo.AddComponent<PlayerMovement>(); player.playerMovement = movement;
        var rendererGo = new GameObject("renderer"); rendererGo.SetActive(false);
        var renderer = rendererGo.AddComponent<PlayerRenderer>(); rendererGo.transform.position = new Vector3(0, 3, 0);
        var root = new GameObject("effects"); root.SetActive(false);
        var dash = new GameObject("dash"); dash.transform.SetParent(root.transform); var dashPs = dash.AddComponent<ParticleSystem>();
        var cloud = new GameObject("cloud"); cloud.transform.SetParent(root.transform); var cloudPs = cloud.AddComponent<ParticleSystem>();
        var dm = dashPs.main; dm.playOnAwake = false; var cm = cloudPs.main; cm.playOnAwake = false;
        var mirror = new GameObject("mirror"); var flex = new GameObject("flex"); mirror.SetActive(false); flex.SetActive(false);
        flex.transform.position = new Vector3(2, 0, 4); mirror.transform.position = new Vector3(7, 0, 9);
        var effects = root.AddComponent<PlayerEffects>(); effects.playerRenderer = renderer; effects.dashPs = dashPs; effects.dashCloudPs = cloudPs; effects.mirrorFx = mirror; effects.flexFx = flex;
        var character = ScriptableObject.CreateInstance<CharacterData>(); character.colliderHeight = 4f;
        root.SetActive(true);
        yield return null;
        try
        {
            ItemMirror.A_MirrorReady?.Invoke(true); PassiveAbilityFlex.A_FlexReady?.Invoke(true);
            if (!mirror.activeSelf || !flex.activeSelf) throw new Exception("Readiness events failed to enable effects");
            renderer.A_CharacterSet?.Invoke(character);
            if (flex.transform.position != new Vector3(2, 6, 4) || mirror.transform.position != flex.transform.position) throw new Exception("Character effect positions did not follow the original placement");
            ItemMirror.A_MirrorReady?.Invoke(false); PassiveAbilityFlex.A_FlexReady?.Invoke(false);
            if (mirror.activeSelf || flex.activeSelf) throw new Exception("Readiness events failed to hide effects");
            var dashFx = typeof(PlayerEffects).GetMethod("DashFx", BindingFlags.Instance | BindingFlags.NonPublic);
            movement.isDashing = true; dashFx.Invoke(effects, null);
            if (!dashPs.isPlaying || !cloudPs.isPlaying) throw new Exception("Dash did not start both particle systems");
            movement.isDashing = false; dashFx.Invoke(effects, null);
            if (dashPs.isPlaying) throw new Exception("Dash particles did not stop");
            UnityEngine.Object.Destroy(root);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            ItemMirror.A_MirrorReady?.Invoke(true); PassiveAbilityFlex.A_FlexReady?.Invoke(true);
            if (mirror.activeSelf || flex.activeSelf) throw new Exception("Destroyed effect retained event listeners");
            Debug.Log("PlayerEffectsVisualPlayCheck passed"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
