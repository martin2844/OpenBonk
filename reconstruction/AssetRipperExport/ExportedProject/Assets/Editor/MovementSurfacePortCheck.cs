using System;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEngine;

public static class MovementSurfacePortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("MovementSurfacePortCheck: " + message); }
    static void Set(PlayerMovement movement, string field, object value) => typeof(PlayerMovement).GetField(field, Private).SetValue(movement, value);
    static object Call(PlayerMovement movement, string method, params object[] args) => typeof(PlayerMovement).GetMethod(method, Private).Invoke(movement, args);

    public static void Run()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0, -0.5f, 0);
        var player = new GameObject("movement surface test");
        player.SetActive(false);
        var body = player.AddComponent<Rigidbody>();
        var movement = player.AddComponent<PlayerMovement>();
        var feet = new GameObject("feet");
        movement.feet = feet.transform;
        movement.orientation = player.transform;
        movement.whatIsGround = 1 << ground.layer;
        Set(movement, "rb", body);
        Physics.SyncTransforms();
        feet.transform.position = new Vector3(0, 0.05f, 0);
        Require((bool)Call(movement, "AreFeetTouchingFloor"), "short ray detects the floor");
        feet.transform.position = new Vector3(0, 0.2f, 0);
        Require(!(bool)Call(movement, "AreFeetTouchingFloor"), "short ray does not detect distant floor");
        body.velocity = new Vector3(2, 3, 4);
        Call(movement, "SetKinematic", true);
        Require(body.velocity == Vector3.zero && !body.useGravity && !body.isKinematic, "suspension clears velocity and disables gravity");
        Call(movement, "SetKinematic", false);
        Require(body.useGravity && !body.isKinematic, "releasing suspension restores gravity");
        Set(movement, "normalVector", Vector3.left);
        Set(movement, "<playerRadius>k__BackingField", 0.45f);
        Require(movement.GetNormal() == Vector3.left && Mathf.Abs(movement.GetPlayerRadius() - 0.45f) < 0.0001f, "movement exposes the actual contact and radius");
        Set(movement, "x", 0.5f); Set(movement, "y", -1f);
        Require((Vector3)Call(movement, "InputVectorRelativeToLook") == new Vector3(0.5f, 0, -1), "input follows orientation");
        Set(movement, "currentMaxSpeed", 12f);
        Require(Mathf.Abs((float)Call(movement, "GetWallClimbSpeed") - 18f) < 0.0001f, "wall speed scales with movement speed");
        Set(movement, "currentMaxSpeed", 2f);
        Require(Mathf.Abs((float)Call(movement, "GetWallClimbSpeed") - 10f) < 0.0001f, "wall speed has a minimum");
        MyTime.time = 10f; Call(movement, "StopWallClimbing");
        Require(Mathf.Abs((float)typeof(PlayerMovement).GetField("canWallClimbAtTime", Private).GetValue(movement) - 10.4f) < 0.0001f, "wall attachment cooldown starts");
        UnityEngine.Object.DestroyImmediate(feet); UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(ground);
        Debug.Log("MovementSurfacePortCheck passed");
    }
}
