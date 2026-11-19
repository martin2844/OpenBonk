using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Optional city layout. It owns only cloned-stage dimensions and decorations;
// Forest and legacy map recipes never enter this path.
public static class OpenBonkStreetLayout
{
    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class Spec
    {
        public float tileWidth = float.NaN, stepHeight = float.NaN, coverage = float.NaN;
        public string[] frontages;
        public bool IsValid()
        {
            if (!DimensionsValid(tileWidth, stepHeight) || !Range(coverage, 0, 1) || frontages == null || frontages.Length < 1 || frontages.Length > 8) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in frontages)
                if (string.IsNullOrWhiteSpace(name) || name.Length > 120 || name.IndexOf('\0') >= 0 || !names.Add(name)) return false;
            return true;
        }
    }

    public sealed class Profile
    {
        public readonly int tileWidth, stepHeight;
        public readonly float coverage;
        public readonly GameObject[] frontages;
        internal readonly Bounds[] bounds;
        public Profile(int tileWidth, int stepHeight, float coverage, GameObject[] frontages)
        {
            if (!DimensionsValid(tileWidth, stepHeight) || !Range(coverage, 0, 1) || frontages == null || frontages.Length < 1 || frontages.Length > 8)
                throw new ArgumentException("Invalid streetscape profile");
            this.tileWidth = tileWidth; this.stepHeight = stepHeight; this.coverage = coverage;
            this.frontages = (GameObject[])frontages.Clone();
            bounds = new Bounds[frontages.Length];
            var unique = new HashSet<GameObject>();
            for (int i = 0; i < frontages.Length; i++)
            {
                var prefab = frontages[i];
                if (prefab == null || !unique.Add(prefab) || !prefab.activeSelf || prefab.transform.childCount != 0 ||
                    prefab.transform.localScale != Vector3.one || prefab.transform.localRotation != Quaternion.identity ||
                    prefab.GetComponent<MeshFilter>()?.sharedMesh is not Mesh mesh ||
                    prefab.GetComponent<MeshRenderer>()?.sharedMaterial == null || mesh.vertexCount < 3 || mesh.vertexCount > 60000)
                    throw new ArgumentException("Street frontages must be small single-mesh prefabs");
                // Authored solid-only boxes preserve awnings and passage apertures.
                // Never accept scripts, rigid bodies, mesh or trigger colliders.
                int boxes = 0;
                foreach (var component in prefab.GetComponents<Component>())
                {
                    if (component is Transform || component is MeshFilter || component is MeshRenderer) continue;
                    if (component is not BoxCollider box || box.isTrigger || ++boxes > 16 || !Finite(box.center) || !Finite(box.size) ||
                        box.size.x <= 0 || box.size.y <= 0 || box.size.z <= 0)
                        throw new ArgumentException("Street frontages allow only explicit solid box colliders");
                    var visual = mesh.bounds;
                    var solid = new Bounds(box.center, box.size);
                    visual.Expand(.25f);
                    if (!visual.Contains(solid.min) || !visual.Contains(solid.max))
                        throw new ArgumentException("Street collider extends beyond visible geometry");
                }
                var b = mesh.bounds;
                if (!Finite(b.center) || !Finite(b.size) || b.size.x < .1f || b.size.z < .1f || b.size.y < .1f ||
                    b.size.x > 8f || b.size.z > 8f || b.size.y > 8f || Mathf.Abs(b.min.y) > .25f)
                    throw new ArgumentException("Street frontage bounds exceed the placement envelope");
                bounds[i] = b;
            }
        }
    }

    public readonly struct Placement
    {
        public readonly GameObject prefab;
        public readonly Vector3 position;
        public readonly Quaternion rotation;
        public readonly float scale;
        public readonly Bounds worldBounds;
        public readonly int tileIndex;
        public Placement(GameObject prefab, Vector3 position, Quaternion rotation, float scale, Bounds worldBounds, int tileIndex)
        { this.prefab = prefab; this.position = position; this.rotation = rotation; this.scale = scale; this.worldBounds = worldBounds; this.tileIndex = tileIndex; }
    }

    public static bool DimensionsValid(float width, float step) => Range(width, 20, 40) && width == Mathf.Floor(width) &&
        Range(step, 2, 12) && step == Mathf.Floor(step) && step <= width * .4f;

    // The 12m crossing remains empty on every tile, including edge-midpoint
    // connections to slopes. Two opposite corner cells contain all decoration.
    public static List<Placement> Plan(IReadOnlyList<GameObject> flats, Profile profile, int seed, MeshCollider terrain)
    {
        if (flats == null || profile == null || terrain == null) throw new ArgumentException("Missing street placement inputs");
        var result = new List<Placement>();
        var random = new System.Random(seed ^ 0x4e494d42);
        const float halfLane = 6f, margin = .25f;
        float cornerSize = profile.tileWidth * .5f - halfLane - margin * 2;
        for (int tileIndex = 1; tileIndex < flats.Count - 1; tileIndex++)
        {
            var tile = flats[tileIndex];
            if (tile == null || random.NextDouble() >= profile.coverage) continue;
            var center = tile.transform.position;
            int diagonal = random.Next(2) == 0 ? -1 : 1;
            for (int corner = 0; corner < 2; corner++)
            {
                int xSign = corner == 0 ? -1 : 1, zSign = xSign * diagonal;
                int selected = random.Next(profile.frontages.Length);
                var b = profile.bounds[selected];
                float scale = Mathf.Min(1f, cornerSize / Mathf.Max(b.size.x, b.size.z));
                // At the smallest accepted block dimensions the frontage would
                // lose human scale, so leave that corner empty rather than squeeze it.
                if (scale < .65f) continue;
                bool faceX = random.Next(2) == 0;
                Vector3 forward = faceX ? Vector3.left * xSign : Vector3.back * zSign;
                Quaternion rotation = Quaternion.LookRotation(forward);
                Vector3 size = b.size * scale;
                if (faceX) { float swap = size.x; size.x = size.z; size.z = swap; }
                Vector3 desired = center + new Vector3(xSign * (halfLane + margin + size.x * .5f), 0,
                    zSign * (halfLane + margin + size.z * .5f));
                Vector3 position = desired - rotation * new Vector3(b.center.x * scale, b.min.y * scale, b.center.z * scale);
                var worldBounds = new Bounds(desired + Vector3.up * (size.y * .5f), size);
                if (!Supported(terrain, worldBounds, center.y)) continue;
                result.Add(new Placement(profile.frontages[selected], position, rotation, scale, worldBounds, tileIndex));
            }
        }
        return result;
    }

    private static bool Supported(MeshCollider terrain, Bounds footprint, float height)
    {
        // Query the authoritative terrain collider directly; newly placed props
        // and gameplay interactables cannot masquerade as supporting floor.
        foreach (float x in new[] { footprint.min.x, footprint.max.x })
        foreach (float z in new[] { footprint.min.z, footprint.max.z })
            if (!terrain.Raycast(new Ray(new Vector3(x, height + 2f, z), Vector3.down), out var hit, 4f) ||
                Mathf.Abs(hit.point.y - height) > .08f || hit.normal.y < .995f) return false;
        return true;
    }

    public static GameObject Generate(ProceduralTileGeneration generator, Profile profile)
    {
        if (profile == null) return null;
        var root = new GameObject("Nimbo streetscape");
        root.transform.SetParent(generator.tilesParent.transform, false);
        Physics.SyncTransforms();
        var plan = Plan(generator.flatTiles, profile, generator.debugSeed, generator.collider.GetComponent<MeshCollider>());
        foreach (var placement in plan)
        {
            var instance = Object.Instantiate(placement.prefab, placement.position, placement.rotation, root.transform);
            instance.transform.localScale = Vector3.one * placement.scale;
        }
        Physics.SyncTransforms();
        Debug.Log($"Streetscape generated: {plan.Count} frontages; tile={profile.tileWidth}, rise={profile.stepHeight}, seed={generator.debugSeed}");
        return root;
    }
    private static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
    private static bool Finite(Vector3 v) => Range(v.x, -100000, 100000) && Range(v.y, -100000, 100000) && Range(v.z, -100000, 100000);
}
