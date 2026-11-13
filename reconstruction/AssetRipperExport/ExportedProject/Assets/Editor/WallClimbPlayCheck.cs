using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Player.Movement;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class WallClimbPlayCheck
{
    const string Pending = "WallClimbPlayCheck.pending";
    static WallClimbPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("wall climb test runner").AddComponent<WallClimbPlayRunner>().Begin();
        }
    }
}

public class WallClimbPlayRunner : MonoBehaviour
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(PlayerMovement movement, string field, object value) => typeof(PlayerMovement).GetField(field, Private).SetValue(movement, value);
    static void Require(bool condition, string message) { if (!condition) throw new Exception("WallClimbPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            MyTime.time = 10f;
            var state = new GameObject("synthetic player state"); state.SetActive(false);
            MyPlayer.Instance = state.AddComponent<MyPlayer>();
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            MyPlayer.Instance.inventory = inventory;
            inventory.passiveAbility = new PassiveAbilityMissing();
            Require(!inventory.HasPassive(EPassive.WallClimb) && inventory.HasPassive(EPassive.None), "missing passive is not wall climb");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "test wall"; wall.layer = LayerMask.NameToLayer("Ground"); wall.transform.position = new Vector3(1f, 1f, 0); wall.transform.localScale = new Vector3(.2f, 5f, 5f);
            var player = new GameObject("wall collision player"); player.layer = LayerMask.NameToLayer("Player"); player.transform.position = new Vector3(.41f, 1f, 0);
            var body = player.AddComponent<Rigidbody>(); body.useGravity = false; body.constraints = RigidbodyConstraints.FreezeRotation;
            var sphere = player.AddComponent<SphereCollider>(); sphere.radius = .5f;
            var movement = player.AddComponent<PlayerMovement>(); movement.orientation = player.transform; movement.whatIsGround = 1 << wall.layer;
            Set(movement, "rb", body); Set(movement, "currentMaxSpeed", 12f); Set(movement, "climbCancel", 5);
            movement.SetBetterInput(new InputState(1f, 0f, 0f, 0f, false, false, false, true));
            int starts = 0; Action started = () => starts++; PlayerMovement.A_StartedWallClimb += started;
            Physics.SyncTransforms(); Physics.Simulate(.02f); Physics.Simulate(.02f);
            Require(movement.wallNormal.x < -.9f && !movement.IsWallClimbing() && starts == 0, "collision without passive does not climb");
            inventory.passiveAbility = new PassiveAbilityWallClimb();
            Require(inventory.HasPassive(EPassive.WallClimb), "wall climb passive is recognized");
            movement.SetBetterInput(new InputState(1f, 0f, 0f, 0f, false, false, false, false));
            body.position = new Vector3(.41f, 1f, 0); body.velocity = Vector3.right; Physics.SyncTransforms(); Physics.Simulate(.02f);
            Require(!movement.IsWallClimbing() && starts == 0, "released wallrun input prevents attachment");
            movement.SetBetterInput(new InputState(1f, 0f, 0f, 0f, false, false, false, true));
            body.position = new Vector3(.41f, 1f, 0); body.velocity = Vector3.right; Physics.SyncTransforms();
            Physics.Simulate(.02f);
            Require(movement.IsWallClimbing() && starts == 1, "wall contact starts climbing once");
            Require(body.velocity.y >= 17.9f && movement.wallNormal.x < -.9f, "climb sets upward speed and stores wall normal");
            Physics.Simulate(.02f);
            Require(starts == 1, "continued contact does not restart climb");
            movement.SetBetterInput(new InputState(1f, 0f, 0f, 0f, false, false, false, false));
            var cancel = typeof(PlayerMovement).GetMethod("UpdateCollisionChecks", Private);
            for (int i = 0; i < 6; i++) { body.position = new Vector3(.41f, 1f, 0); body.velocity = Vector3.right; Physics.SyncTransforms(); Physics.Simulate(.02f); cancel.Invoke(movement, null); }
            Require(!movement.IsWallClimbing() && starts == 1, "releasing wallrun allows climb state to expire without restarting");
            inventory.passiveAbility = new PassiveAbilityVampire();
            Require(inventory.HasPassive(EPassive.Warrior) && !inventory.HasPassive(EPassive.Vampire), "passive identity follows original compiled method");
            PlayerMovement.A_StartedWallClimb -= started;
            Debug.Log("WallClimbPlayCheck passed: passive gate, held/released input, real wall contact, climb speed and start event");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
