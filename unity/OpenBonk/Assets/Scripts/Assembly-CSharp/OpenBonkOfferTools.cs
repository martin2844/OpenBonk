// Virtual counts: normal inventory counts remain untouched while enabled.
public static class OpenBonkOfferTools
{
    public const int VirtualCount = 9999;
    public static bool Enabled { get; private set; }
    public static void SetEnabled(bool enabled) { Enabled = enabled; }
    public static int Available(int count) => Enabled ? System.Math.Max(VirtualCount, count) : count;
    public static int Consume(int count) => Enabled ? count : count - 1;
}
