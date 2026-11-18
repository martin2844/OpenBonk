// Separate opt-in policies; a false result must never enable either feature.
public static class OpenBonkOptionalUiPolicy
{
    public static void Apply(string hook, bool enabled)
    {
        if (!enabled) return;
        if (hook == "item_lab") OpenBonkItemGrantPolicy.SetEnabled(true);
        else if (hook == "chest_tracker") OpenBonkChestTracker.SetEnabled(true);
    }
}
