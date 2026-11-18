// A false result must never enable an opt-in feature.
public static class OpenBonkOptionalUiPolicy
{
    public static bool TryApplyMinimap(string hook, double value)
    {
        return hook == "minimap_zoom" && OpenBonkMinimapPolicy.TrySetZoom(value);
    }

    public static void Apply(string hook, bool enabled)
    {
        if (!enabled) return;
        if (hook == "item_lab") OpenBonkItemGrantPolicy.SetEnabled(true);
        else if (hook == "chest_tracker") OpenBonkChestTracker.SetEnabled(true);
        else if (hook == "run_metrics") OpenBonkRunMetrics.SetEnabled(true);
        else if (hook == "retry_on_death") GameManager.RetryEnabled = true;
    }
}
