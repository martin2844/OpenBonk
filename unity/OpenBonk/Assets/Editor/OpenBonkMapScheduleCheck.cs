#if UNITY_EDITOR
using System;
using Assets.Scripts.Game.Spawning.New.Timelines;
using UnityEditor;
using UnityEngine;

// Uses the shipped Forest assets as well as explicit schedule constants. This
// catches a flattened duration array even if its total still equals 27 minutes.
public static class OpenBonkMapScheduleCheck
{
    private static readonly int[][] Seconds = { new[] { 180, 240, 420, 480 }, new[] { 120, 180, 360, 420 }, new[] { 90, 150, 240, 300 } };
    private static readonly int[][] Durations = { new[] { 30, 30, 30, 30 }, new[] { 15, 30, 30, 45 }, new[] { 30, 30, 30, 45 } };
    private static readonly ETimelineEvent[] Kinds = { ETimelineEvent.EMiniBoss, ETimelineEvent.ESwarm, ETimelineEvent.ESwarm, ETimelineEvent.EMiniBoss };

    public static void Run()
    {
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        Require(forest != null && forest.stages.Length == 3 && forest.stageDuration == 600, "native map-level density duration");
        for (int i = 0; i < 3; i++) CheckTimeline(forest.stages[i].stageTimeline, i, "Forest");
        var icon = new Texture2D(2, 2);
        var recipe = new OpenBonkProceduralMapRecipe
        {
            name = "Schedule check", description = "Schedule check", tileGridSize = 15,
            volatility = .8f, slopeStrength = 0, flatMapBias = 0, stageDuration = 600,
            stageDurations = new[] { 600f, 540f, 480f },
        };
        try
        {
            using (var first = OpenBonkProceduralMapBuild.Create(forest, recipe, icon))
            using (var second = OpenBonkProceduralMapBuild.Create(forest, recipe, icon))
            {
                Require(first.Map.stageDuration == 600 && second.Map.stageDuration == 600, "native density denominator retained");
                for (int i = 0; i < 3; i++)
                {
                    var source = forest.stages[i].stageTimeline;
                    var clone = first.Map.stages[i].stageTimeline;
                    var other = second.Map.stages[i].stageTimeline;
                    CheckTimeline(clone, i, "Nimbo"); CheckTimeline(other, i, "second build");
                    Require(!ReferenceEquals(source, clone) && !ReferenceEquals(clone, other), "independent timelines");
                    for (int e = 0; e < Kinds.Length; e++)
                        Require(!ReferenceEquals(source.events[e], clone.events[e]) && !ReferenceEquals(clone.events[e], other.events[e]), "independent event objects");
                    clone.events[0].timeMinutes = 0;
                    clone.events[0].duration = 999;
                    clone.checkNewEnemyInterval = 5;
                    CheckTimeline(source, i, "source after clone mutation");
                    CheckTimeline(other, i, "second build after clone mutation");
                }
                recipe.stageDurations[0] = 60;
                Require(first.Map.stages[0].stageTimeline.stageTime == 600 && second.Map.stages[0].stageTimeline.stageTime == 600,
                    "later caller array changes cannot alter constructed maps");
            }
            recipe.stageDurations = null;
            recipe.stageDuration = 300;
            using (var legacy = OpenBonkProceduralMapBuild.Create(forest, recipe, icon))
            {
                Require(legacy.Map.stageDuration == 300, "legacy scalar map duration");
                for (int i = 0; i < 3; i++)
                {
                    var source = forest.stages[i].stageTimeline; var clone = legacy.Map.stages[i].stageTimeline;
                    float scale = 300 / source.stageTime;
                    Require(clone.stageTime == 300 && Mathf.Approximately(clone.checkNewEnemyInterval, source.checkNewEnemyInterval * scale), "legacy scalar stage scaling");
                    for (int e = 0; e < Kinds.Length; e++)
                        Require(Mathf.Approximately(clone.events[e].timeMinutes, source.events[e].timeMinutes * scale) &&
                            Mathf.Approximately(clone.events[e].duration, source.events[e].duration * scale), "legacy event scaling");
                }
            }
            float[][] invalid =
            {
                Array.Empty<float>(), new[] { 600f, 540f }, new[] { 600f, 540f, 480f, 420f },
                new[] { 59f, 540f, 480f }, new[] { 600f, 1801f, 480f },
                new[] { float.NaN, 540f, 480f }, new[] { 600f, float.PositiveInfinity, 480f },
                new[] { 600f, 540f, float.NegativeInfinity },
            };
            foreach (var durations in invalid)
            {
                recipe.stageDurations = durations;
                bool rejected = false;
                try { using var unexpected = OpenBonkProceduralMapBuild.Create(forest, recipe, icon); }
                catch (ArgumentException) { rejected = true; }
                Require(rejected, "malformed per-stage duration array rejected");
            }
            for (int i = 0; i < 3; i++) CheckTimeline(forest.stages[i].stageTimeline, i, "unchanged Forest after checks");
            Debug.Log("OpenBonk map schedule PASS: native 600/540/480 clocks, exact four-event schedules and durations, 60-second reinforcements, 600-second density denominator, independent clones, legacy scalar behavior and eight invalid arrays");
        }
        finally { UnityEngine.Object.DestroyImmediate(icon); }
    }

    private static void CheckTimeline(StageTimeline timeline, int stage, string label)
    {
        Require(timeline != null && timeline.stageTime == 600 - stage * 60 && timeline.checkNewEnemyInterval == 60 &&
            timeline.events != null && timeline.events.Count == Kinds.Length, label + " stage duration/events");
        for (int e = 0; e < Kinds.Length; e++)
        {
            var entry = timeline.events[e];
            Require(entry != null && entry.eTimelineEvent == Kinds[e] && entry.GetTimeSeconds() == Seconds[stage][e] &&
                entry.duration == Durations[stage][e], label + " exact stage " + (stage + 1) + " event " + e);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception("Map schedule: " + message); }
}
#endif
