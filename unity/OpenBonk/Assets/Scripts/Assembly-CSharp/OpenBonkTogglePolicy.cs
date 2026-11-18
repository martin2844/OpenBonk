using Assets.Scripts.Saves___Serialization.Progression.Achievements;

// Read-only override to the native Toggler eligibility rules. It does not
// unlock content or change data assets; native user choices still persist.
public static class OpenBonkTogglePolicy
{
    public static bool Enabled { get; private set; }
    public static void SetEnabled(bool value) { Enabled = value; }
    public static bool AlwaysToggle(UnlockableBase value)
    {
        return value != null && (value.canAlwaysToggle ||
            (Enabled && (value is WeaponData || value is TomeData || value is ItemData)));
    }
}
