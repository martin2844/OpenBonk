using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyDashSetupPortCheck
{
    static T Field<T>(EnemyMovementRb movement, string name) => (T)typeof(EnemyMovementRb).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(movement);
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        var obj = new GameObject("enemy dash setup check");
        var body = obj.AddComponent<Rigidbody>(); body.isKinematic = true;
        var movement = obj.AddComponent<EnemyMovementRb>(); movement.rb = body;
        float oldTime = MyTime.time;
        try
        {
            MyTime.time = 12f;
            movement.DashStart(new Vector3(3f, 4f, 4f), 2.5f, 17f);
            Require(!body.isKinematic, "dash makes body dynamic");
            Require(Field<bool>(movement, "dashing"), "dash enabled");
            Require(Mathf.Abs(Field<float>(movement, "dashStopTime") - 14.5f) < .0001f, "dash deadline");
            Require(Mathf.Abs(Field<float>(movement, "dashSpeed") - 17f) < .0001f, "dash speed");
            Require(Vector3.Distance(Field<Vector3>(movement, "dashDirection"), new Vector3(.6f, 0f, .8f)) < .0001f, "horizontal direction normalized");
            Require(!Field<bool>(movement, "isDashingWall"), "wall state reset");
            movement.SetDashDirection(Vector3.left);
            Require(Vector3.Distance(Field<Vector3>(movement, "dashDirection"), new Vector3(.6f, 0f, .8f)) < .0001f, "rotation does not replace travel direction");
            movement.StopDash(); Require(!Field<bool>(movement, "dashing"), "dash stopped");
            Debug.Log("EnemyDashSetupPortCheck passed: initialization, horizontal direction, deadline, speed and stop");
        }
        finally { MyTime.time = oldTime; UnityEngine.Object.DestroyImmediate(obj); }
    }
}
