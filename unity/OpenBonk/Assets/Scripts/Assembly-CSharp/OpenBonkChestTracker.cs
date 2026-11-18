using System.Collections.Generic;

// Read-only chest observation. IDs are scoped to a generated map. A chest
// receiving duplicate successful callbacks is counted as opened once.
public static class OpenBonkChestTracker
{
    private static readonly Dictionary<int, bool> chests = new Dictionary<int, bool>();
    private static int total, opened;
    public static bool Enabled { get; private set; }
    public static int Total => total;
    public static int Unopened => total - opened;
    public static int Revision { get; private set; }

    public static void SetEnabled(bool value)
    {
        if (Enabled == value) return;
        Enabled = value;
        MapGenerationController.A_PreGeneration -= Reset;
        if (value) MapGenerationController.A_PreGeneration += Reset;
        Reset();
    }
    public static void Reset() { chests.Clear(); total = opened = 0; Revision++; }
    public static void Register(int id)
    {
        if (!Enabled || chests.ContainsKey(id)) return;
        chests.Add(id, false); total++; Revision++;
    }
    public static void Opened(int id)
    {
        if (!Enabled || !chests.TryGetValue(id, out bool used) || used) return;
        chests[id] = true; opened++; Revision++;
    }
    public static void Disabled(int id)
    {
        if (!Enabled || !chests.TryGetValue(id, out bool used)) return;
        // Opened objects remain in the map total; unopened removed objects do not.
        if (!used) { chests.Remove(id); total--; Revision++; }
    }
}
