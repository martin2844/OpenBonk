using System;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyPenetrationPortCheck
{
    public static void Run()
    {
        float previous = MyTime.finalSwarmTimer;
        try
        {
            Check(0f, 0f);
            Check(3599f, 0f);
            Check(3600f, 0f);
            Check(3659.9f, 0f);
            Check(3660f, .0002f);
            Check(3719.9f, .0002f);
            Check(3720f, .0004f);
            Check(7200f, .012f);
            Debug.Log("ENEMY_PENETRATION_PORT_CHECK_OK");
        }
        finally { MyTime.finalSwarmTimer = previous; }
    }

    private static void Check(float time, float expected)
    {
        MyTime.finalSwarmTimer = time;
        float actual = EnemyStats.GetEvasionAndArmorPenetration();
        if (Mathf.Abs(actual - expected) > .0000001f)
            throw new Exception($"time={time}: expected {expected}, got {actual}");
    }
}
