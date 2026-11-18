using System;

// Bounded, allocation-free buckets; UI and callers provide the game clock.
public static class OpenBonkRunMetrics
{
    public struct Snapshot { public double XpPerSecond, GoldPerSecond, DamagePerSecond; }
    private struct Bucket { public long Tick; public double Xp, Gold, Damage; public bool Used; }
    private static readonly Bucket[] buckets = new Bucket[32];
    private static long latestTick = long.MinValue;
    public static bool Enabled { get; private set; }

    public static void SetEnabled(bool enabled) { Enabled = enabled; Reset(); }
    public static void Reset() { Array.Clear(buckets, 0, buckets.Length); latestTick = long.MinValue; }
    private static bool Tick(double time, out long tick)
    {
        tick = 0;
        if (double.IsNaN(time) || double.IsInfinity(time) || time < 0 || time > 1e12) return false;
        tick = (long)Math.Floor(time * 10.0);
        if (latestTick != long.MinValue && tick < latestTick) Reset();
        latestTick = tick;
        return true;
    }
    private static void Add(double time, double amount, int kind)
    {
        if (!Enabled || double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0 || !Tick(time, out long tick)) return;
        int index = (int)(tick % buckets.Length);
        if (!buckets[index].Used || buckets[index].Tick != tick) buckets[index] = new Bucket { Used = true, Tick = tick };
        if (kind == 0) buckets[index].Xp += amount;
        else if (kind == 1) buckets[index].Gold += amount;
        else buckets[index].Damage += amount;
    }
    public static void AddXp(double time, double amount) => Add(time, amount, 0);
    public static void AddGold(double time, double amount) => Add(time, amount, 1);
    public static void AddDamage(double time, double amount) => Add(time, amount, 2);
    public static Snapshot Get(double time)
    {
        var result = new Snapshot();
        if (!Enabled || !Tick(time, out long now)) return result;
        for (int i = 0; i < buckets.Length; i++)
        {
            Bucket b = buckets[i];
            if (!b.Used || b.Tick > now) continue;
            long age = now - b.Tick;
            if (age < 10) { result.XpPerSecond += b.Xp; result.GoldPerSecond += b.Gold; }
            if (age < 30) result.DamagePerSecond += b.Damage / 3.0;
        }
        return result;
    }
}
