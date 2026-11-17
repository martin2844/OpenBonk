// Enabled only by a validated, enabled mod on application startup.
public static class OpenBonkPickupPolicy
{
    public static bool CollectXpImmediately { get; private set; }
    public static void SetCollectXpImmediately(bool enabled) { CollectXpImmediately = enabled; }
}
