using System;
using Assets.Scripts.Game.MapGeneration;
using UnityEngine;

// Validated decorative prefabs are appended to cloned stages only.
public static class OpenBonkMapObjects
{
    // Distinguishes mod-authored decorations from the game's original entries.
    public sealed class DecorativeObject : RandomMapObject { }
    public sealed class Placement
    {
        public GameObject prefab;
        public int amount;
        public int[] stages;
        public float scaleMin, scaleMax, checkRadius, maxSlopeAngle, upOffset;
    }

    public static void AddToStages(OpenBonkExternalMapData map, Placement[] placements)
    {
        if (map == null || map.stages == null || placements == null || placements.Length == 0 || placements.Length > 8) throw new ArgumentException("Invalid map objects");
        var additions = new RandomMapObject[placements.Length];
        var stageSelection = new bool[placements.Length][];
        for (int i = 0; i < placements.Length; i++)
        {
            var p = placements[i];
            if (p == null || p.prefab == null || p.prefab.GetComponentsInChildren<Renderer>(true).Length == 0 || p.amount < 1 || p.amount > 64 ||
                !Range(p.scaleMin, .1f, 10f) || !Range(p.scaleMax, p.scaleMin, 10f) || !Range(p.checkRadius, .1f, 20f) ||
                !Range(p.maxSlopeAngle, 0, 60) || !Range(p.upOffset, -10, 10)) throw new ArgumentException("Invalid map object placement");
            var selected = new bool[map.stages.Length];
            if (p.stages == null) { for (int s = 0; s < selected.Length; s++) selected[s] = true; }
            else
            {
                if (p.stages.Length == 0 || p.stages.Length > selected.Length) throw new ArgumentException("Invalid map object stages");
                foreach (int stage in p.stages)
                {
                    if (stage < 1 || stage > selected.Length || selected[stage - 1]) throw new ArgumentException("Invalid map object stages");
                    selected[stage - 1] = true;
                }
            }
            stageSelection[i] = selected;
            additions[i] = new DecorativeObject { prefabs = new[] { p.prefab }, amount = p.amount, scaleMin = p.scaleMin, scaleMax = p.scaleMax,
                checkRadius = p.checkRadius, maxSlopeAngle = p.maxSlopeAngle, upOffset = p.upOffset, randomRotationVector = Vector3.up };
        }
        // Allocate every array before mutating any stage, so validation is transactional.
        var arrays = new RandomMapObject[map.stages.Length][];
        for (int i = 0; i < map.stages.Length; i++)
        {
            var stage = map.stages[i]; if (stage == null) throw new ArgumentException("Missing map stage");
            var original = stage.randomMapObjects ?? Array.Empty<RandomMapObject>();
            int added = 0; for (int j = 0; j < additions.Length; j++) if (stageSelection[j][i]) added++;
            arrays[i] = new RandomMapObject[original.Length + added];
            Array.Copy(original, arrays[i], original.Length);
            int dest = original.Length; for (int j = 0; j < additions.Length; j++) if (stageSelection[j][i]) arrays[i][dest++] = additions[j];
        }
        for (int i = 0; i < arrays.Length; i++) map.stages[i].randomMapObjects = arrays[i];
    }
    private static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
}
