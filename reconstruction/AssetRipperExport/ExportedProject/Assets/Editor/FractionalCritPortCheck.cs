using System;
using Assets.Scripts.Game.Combat;
using UnityEngine;
using Utility;

public static class FractionalCritPortCheck
{
    public static void Run()
    {
        var oldRandom = MyRandom.random;
        try
        {
            foreach (float chance in new[] { 0f, .001f, .1f, .5f, .999f, 1f, 1.25f, 1.75f, 2f, 2.99f, 3.25f, 5.75f })
            {
                const int seed = 731935;
                var expectedRandom = new System.Random(seed);
                MyRandom.random = new System.Random(seed);
                for (int i = 0; i < 200; i++)
                {
                    int levels = Mathf.FloorToInt(chance);
                    if (expectedRandom.NextDouble() < chance - levels) levels++;
                    float expectedMultiplier = levels == 0 ? 1f : levels == 1 ? 2f : 1f + levels + (levels * .5f) * (levels * .5f);
                    bool crit = DamageUtility.GetCritDamageMultiplier(chance, out float multiplier);
                    if (crit != (levels != 0) || Mathf.Abs(multiplier - expectedMultiplier) > .00001f)
                        throw new Exception($"chance={chance} sample={i}: levels={levels} multiplier={multiplier} expected={expectedMultiplier}");
                }
                if (MyRandom.random.NextDouble() != expectedRandom.NextDouble()) throw new Exception("random state differs at " + chance);
            }
            Debug.Log("FractionalCritPortCheck passed: 2400 seeded rolls across zero, fractional and multiple guaranteed critical levels");
        }
        finally { MyRandom.random = oldRandom; }
    }
}
