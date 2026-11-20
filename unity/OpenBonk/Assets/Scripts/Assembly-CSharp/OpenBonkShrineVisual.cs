using UnityEngine;

// Puts a mod-supplied statue in place of a native shrine mesh. Colliders,
// interaction, icons and single-use rewards stay on the original object.
internal static class OpenBonkShrineVisual
{
    internal static GameObject Replace(Component host, GameObject visual, string name)
    {
        if (host == null || visual == null) return null;
        MeshFilter original = null;
        float volume = 0;
        foreach (var filter in host.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null || renderer == null || !renderer.enabled) continue;
            // Rotated icon quads acquire a large world-space AABB on slopes.
            // Rank solid mesh bounds instead, so an icon cannot become the head.
            var size = filter.sharedMesh.bounds.size;
            var meshScale = filter.transform.lossyScale;
            float candidate = Mathf.Abs(size.x * size.y * size.z * meshScale.x * meshScale.y * meshScale.z);
            if (candidate > volume) { original = filter; volume = candidate; }
        }
        var source = visual.GetComponent<MeshFilter>()?.sharedMesh;
        if (original == null || source == null) return null;
        var envelope = original.sharedMesh.bounds;
        var bounds = source.bounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) return null;
        // The native shrine mesh uses +Z up; imported mod visuals use +Y up.
        var rotation = Quaternion.Euler(90f, 0, 0);
        var rotated = new Bounds(rotation * bounds.center, new Vector3(bounds.size.x, bounds.size.z, bounds.size.y));
        float scale = Mathf.Min(envelope.size.x / rotated.size.x,
            envelope.size.y / rotated.size.y, envelope.size.z / rotated.size.z);
        var instance = Object.Instantiate(visual, original.transform, false);
        instance.name = name;
        instance.layer = original.gameObject.layer;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = Vector3.one * scale;
        instance.transform.localPosition = new Vector3(envelope.center.x - rotated.center.x * scale,
            envelope.center.y - rotated.center.y * scale, envelope.min.z - rotated.min.z * scale);
        original.GetComponent<MeshRenderer>().enabled = false;
        return instance;
    }
}
