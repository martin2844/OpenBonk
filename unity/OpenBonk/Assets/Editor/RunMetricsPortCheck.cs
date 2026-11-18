using System;
using UnityEngine;

public static class RunMetricsPortCheck
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static bool Near(double a, double b) => Math.Abs(a - b) < .00001;
    public static void Run()
    {
        try
        {
            OpenBonkRunMetrics.SetEnabled(false); OpenBonkChestTracker.SetEnabled(false); OpenBonkItemGrantPolicy.SetEnabled(false);
            OpenBonkRunMetrics.AddDamage(1, 900);
            OpenBonkOptionalUiPolicy.Apply("run_metrics", false);
            Check(!OpenBonkRunMetrics.Enabled && Near(OpenBonkRunMetrics.Get(1).DamagePerSecond, 0), "disabled and false policy");
            OpenBonkOptionalUiPolicy.Apply("run_metrics", true);
            Check(OpenBonkRunMetrics.Enabled && !OpenBonkChestTracker.Enabled && !OpenBonkItemGrantPolicy.Enabled, "policy isolation");
            OpenBonkRunMetrics.AddXp(10, 12); OpenBonkRunMetrics.AddGold(10, 7); OpenBonkRunMetrics.AddDamage(10, 30);
            OpenBonkRunMetrics.AddXp(10, -3); OpenBonkRunMetrics.AddGold(10, double.NaN); OpenBonkRunMetrics.AddDamage(10, double.PositiveInfinity);
            var snapshot = OpenBonkRunMetrics.Get(10.5);
            Check(Near(snapshot.XpPerSecond, 12) && Near(snapshot.GoldPerSecond, 7) && Near(snapshot.DamagePerSecond, 10), "rates and invalid values");
            snapshot = OpenBonkRunMetrics.Get(11.0);
            Check(Near(snapshot.XpPerSecond, 0) && Near(snapshot.GoldPerSecond, 0) && Near(snapshot.DamagePerSecond, 10), "resource expiry");
            snapshot = OpenBonkRunMetrics.Get(13.0);
            Check(Near(snapshot.DamagePerSecond, 0), "damage expiry");
            OpenBonkRunMetrics.AddDamage(15, 15); Check(Near(OpenBonkRunMetrics.Get(1).DamagePerSecond, 0), "clock reset");
            for (int i = 0; i < 100000; i++) OpenBonkRunMetrics.AddDamage(20, 1);
            Check(Near(OpenBonkRunMetrics.Get(20).DamagePerSecond, 100000.0 / 3.0), "bounded high frequency");
            OpenBonkRunMetrics.Reset(); Check(Near(OpenBonkRunMetrics.Get(20).DamagePerSecond, 0), "run reset");
            OpenBonkRunMetrics.SetEnabled(false); OpenBonkRunMetrics.AddXp(20, 20);
            Check(Near(OpenBonkRunMetrics.Get(20).XpPerSecond, 0), "disabled reset");
            Debug.Log("RunMetricsPortCheck passed: windows, invalid input, reset, isolation and bounded high-frequency events");
        }
        finally { OpenBonkRunMetrics.SetEnabled(false); }
    }
}
