using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public static class RailMovementPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object instance, string name, object value) => instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    static object Get(object instance, string name) => instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
    static void Invoke(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, args);
    public static void Run()
    {
        var root = new GameObject("RailMovementCheck");
        float oldTime = MyTime.time, oldFixedDelta = MyTime.fixedDeltaTime;
        var changes = new List<bool>(); Action<bool> onGrind = changes.Add;
        PlayerMovement.A_ToggleGrind += onGrind;
        try
        {
            MyTime.time = 10f; MyTime.fixedDeltaTime = Time.fixedDeltaTime;
            var player = new GameObject("Player"); player.transform.SetParent(root.transform); player.transform.position = new Vector3(0, 1, 5);
            var rigidbody = player.AddComponent<Rigidbody>(); rigidbody.useGravity = false;
            var playerCollider = player.AddComponent<CapsuleCollider>();
            var movement = player.AddComponent<PlayerMovement>();
            Set(movement, "rb", rigidbody); Set(movement, "playerCollider", playerCollider);
            Set(movement, "feetHeight", new Vector3(0, 1, 0)); Set(movement, "standingFeetOffset", new Vector3(0, 1, 0));
            Set(movement, "currentMaxSpeed", 8f);
            typeof(PlayerMovement).GetProperty("playerRadius").GetSetMethod(true).Invoke(movement, new object[] { 0.5f });
            rigidbody.velocity = Vector3.forward * 3f;
            var effects = player.AddComponent<PlayerSfxs>();
            effects.sourceGrindAction = player.AddComponent<RandomSfx>(); effects.sourceGrindLoop = player.AddComponent<RandomSfx>();
            effects.grindFx = new GameObject("GrindFx"); effects.grindFx.transform.SetParent(player.transform); effects.grindFx.SetActive(false);
            movement.playerSfx = effects;
            var railObject = new GameObject("Rail"); railObject.transform.SetParent(root.transform); railObject.tag = "Rail";
            var rail = railObject.AddComponent<Rail>(); rail.splineContainer = railObject.AddComponent<SplineContainer>();
            rail.splineContainer.Spline.Add(new BezierKnot(new float3(0, 0, 0)));
            rail.splineContainer.Spline.Add(new BezierKnot(new float3(0, 0, 10)));
            var railCollider = railObject.AddComponent<BoxCollider>(); railCollider.center = new Vector3(0, 0.2f, 5); railCollider.size = new Vector3(0.4f, 0.4f, 10f);
            rail.renderCollider = railCollider;
            var detector = player.AddComponent<DetectRails>(); detector.whatIsRails = 1 << railObject.layer;
            Invoke(detector, "Awake"); Physics.SyncTransforms();
            Invoke(detector, "FixedUpdate");
            Require(movement.IsGrinding() && rigidbody.isKinematic && effects.grindFx.activeSelf, "detector enters rail");
            Require(Mathf.Abs((float)Get(movement, "progress") - 0.5f) < 0.02f && Mathf.Abs((float)Get(movement, "railSpeed") - 12f) < 0.01f, "nearest point and minimum rail speed");
			Require(Mathf.Abs((float)Get(movement, "canJumpOffRailTime") - (MyTime.time + Time.fixedDeltaTime)) < 0.0001f, "initial jump delay");
            Require(changes.Count == 1 && changes[0], "grind-start event");
            float before = (float)Get(movement, "progress"); Invoke(movement, "RailMovement");
            Require((float)Get(movement, "progress") > before, "advance along rail");
            Set(movement, "progress", 0.9999f); Invoke(movement, "RailMovement");
            Require(!movement.IsGrinding() && !rigidbody.isKinematic && !effects.grindFx.activeSelf, "dismount at end");
            Require(rigidbody.velocity.y > 4.9f && rigidbody.velocity.z > 10f, "dismount velocity");
            Require(rail.IsOnCooldown() && Physics.GetIgnoreCollision(railCollider, playerCollider) && changes.Count == 2 && !changes[1], "cooldown and stop event");
            MyTime.time += 0.6f; Invoke(rail, "FixedUpdate");
            Require(!rail.IsOnCooldown() && !Physics.GetIgnoreCollision(railCollider, playerCollider), "collision restored");
			rigidbody.position = new Vector3(0, 1, 5); rigidbody.velocity = Vector3.back * 4f;
			movement.StartRail(rail);
			Require((float)Get(movement, "railDirectionMultiplier") == -1f, "reverse entry direction");
			Invoke(detector, "OnDamage", null, null, false);
			Require(!movement.IsGrinding() && rigidbody.velocity.z < 0f && changes.Count == 4 && !changes[3], "damage dismounts in travel direction");
            Invoke(detector, "OnDestroy");
            Debug.Log("RailMovementPortCheck passed: detection, entry, travel, dismount, cooldown and collision restoration");
        }
        finally
        {
            PlayerMovement.A_ToggleGrind -= onGrind;
            MyTime.time = oldTime; MyTime.fixedDeltaTime = oldFixedDelta;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
