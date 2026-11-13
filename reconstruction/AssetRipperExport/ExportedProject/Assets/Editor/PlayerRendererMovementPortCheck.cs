using System;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public static class PlayerRendererMovementPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object instance, string name, object value) => instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    static void Invoke(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, args);
    public static void Run()
    {
        const string controllerPath = "Assets/Editor/PlayerRendererMovementPortCheck-temp.controller";
        var root = new GameObject("PlayerRendererMovementCheck");
        var oldCamera = PlayerCamera.Instance; var oldGame = GameManager.Instance;
        float oldTime = MyTime.time; bool oldPaused = MyTime.paused;
        try
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            foreach (string parameter in new[] { "moving", "grounded", "jumping", "grinding" }) controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
            var movementObject = new GameObject("Movement"); movementObject.transform.SetParent(root.transform);
            var movement = movementObject.AddComponent<PlayerMovement>();
            movement.orientation = movementObject.transform;
			Set(movement, "climbCancel", 5);
            movement.feet = new GameObject("feet").transform; movement.feet.SetParent(movementObject.transform);
            movement.feet.position = new Vector3(3, 2, 1);
            var playerObject = new GameObject("Visual"); playerObject.transform.SetParent(root.transform);
            var player = playerObject.AddComponent<PlayerRenderer>();
            var animator = playerObject.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.Rebind();
            player.animator = animator; player.playerMovement = movement;
            Set(player, "activeMaterials", new Material[0]); Set(player, "rendererObject", playerObject);
            PlayerCamera.Instance = null; GameManager.Instance = null; MyTime.paused = false; MyTime.time = 5f;
            player.ForceRotation(Vector3.right);
            Require(Vector3.Angle(player.transform.forward, Vector3.right) < 0.01f, "forced facing");
            Set(movement, "x", 1f);
            Require(movement.IsHoldingMovement(), "movement input query");
            Invoke(player, "Update");
            Require(player.transform.position == movement.feet.position && animator.GetBool("moving"), "follow feet and moving animation");
            Set(movement, "x", 0f); MyTime.time = 6f; Invoke(player, "Update");
            MyTime.time = 6.2f; Invoke(player, "Update");
            Require(!animator.GetBool("moving"), "delayed idle animation");
            Invoke(player, "OnPause", true); Require(animator.speed == 0f, "paused animation");
            Invoke(player, "OnPause", false); Require(animator.speed == 1f, "resumed animation");
            Set(movement, "climbCancel", 1); Set(movement, "climbCancelTicks", 2);
            Require(movement.IsWallClimbing(), "wall climb query");
            var railObject = new GameObject("Rail"); railObject.transform.SetParent(root.transform);
            var rail = railObject.AddComponent<Rail>(); rail.splineContainer = railObject.AddComponent<SplineContainer>();
            rail.splineContainer.Spline.Add(new BezierKnot(new float3(0, 0, 0)));
            rail.splineContainer.Spline.Add(new BezierKnot(new float3(0, 0, 10)));
            Set(movement, "rail", rail); Set(movement, "progress", 0.5f); Set(movement, "railDirectionMultiplier", -1f);
            Require(movement.IsGrinding() && Vector3.Dot(movement.GetGrindNormal(), Vector3.up) > 0.9f && Vector3.Dot(movement.GetGrindDirection().normalized, Vector3.back) > 0.9f, "rail direction and normal");
            Debug.Log("PlayerRendererMovementPortCheck passed: facing, follow, movement, idle, pause, wall-climb and rail queries");
        }
        finally
        {
            MyTime.time = oldTime; MyTime.paused = oldPaused; PlayerCamera.Instance = oldCamera; GameManager.Instance = oldGame;
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.DeleteAsset(controllerPath);
        }
    }
}
