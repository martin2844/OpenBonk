// This override changes query results only; it never writes progression data.
public static class OpenBonkUnlockPolicy
{
    public static bool Enabled { get; private set; }
    public static void SetEnabled(bool enabled) { Enabled = enabled; }
}
