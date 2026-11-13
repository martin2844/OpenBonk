using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyPausePortCheck
{
    static void Require(bool value, string reason) { if (!value) throw new Exception("EnemyPausePortCheck: " + reason); }
    public static void Run()
    {
        var previousPauseEvent = MyTime.A_Pause;
        bool previousPaused = MyTime.paused;
        var previousSimulation = Physics.simulationMode;
        var obj = new GameObject("pause test enemy"); obj.SetActive(false);
        try
        {
            MyTime.A_Pause = null;
            var enemy = obj.AddComponent<Enemy>();
            var movement = obj.AddComponent<EnemyMovementRb>();
            var rb = obj.AddComponent<Rigidbody>();
            movement.rb = rb; enemy.enemyMovement = movement;
            var awake = typeof(Enemy).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            var destroy = typeof(Enemy).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
            awake.Invoke(enemy, null);
            Require(MyTime.A_Pause != null, "subscribes to pause");
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            MyTime.Pause();
            Require(MyTime.paused && rb.constraints == RigidbodyConstraints.FreezeAll && Physics.simulationMode == SimulationMode.Script, "pause freezes enemy body");
            MyTime.Unpause();
            Require(!MyTime.paused && rb.constraints == RigidbodyConstraints.FreezeRotation && Physics.simulationMode == SimulationMode.FixedUpdate, "unpause restores rotation constraint");
            destroy.Invoke(enemy, null);
            Require(MyTime.A_Pause == null, "removes pause subscription");
            Debug.Log("EnemyPausePortCheck passed: pause, unpause and unsubscribe");
        }
        finally
        {
            MyTime.A_Pause = previousPauseEvent; MyTime.paused = previousPaused; Physics.simulationMode = previousSimulation;
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
