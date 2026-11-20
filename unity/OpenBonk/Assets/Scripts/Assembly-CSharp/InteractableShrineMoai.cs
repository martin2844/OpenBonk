using UnityEngine;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;

public class InteractableShrineMoai : BaseInteractable
{
	public GameObject minimapIcon;

	public GameObject alertIcon;

	public bool done;

	public static string debugName = "Moais";

    private bool visualReplaced;

    protected new void Start()
    {
        if (Assets.Scripts.Managers.MapController.currentMap is OpenBonkExternalMapData map &&
            Assets.Scripts.Managers.MapController.currentStage != null &&
            map.moaiVisuals.TryGetValue(Assets.Scripts.Managers.MapController.currentStage, out var visual))
            ReplaceVisual(visual);
        base.Start();
    }

    // Replace presentation only. Native colliders, interaction, icons and
    // single-use rewards remain on the original object.
    internal GameObject ReplaceVisual(GameObject visual)
    {
        if (visual == null || visualReplaced) return null;
        MeshFilter original = null;
        float volume = 0;
        foreach (var filter in GetComponentsInChildren<MeshFilter>())
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
        var instance = Instantiate(visual, original.transform, false);
        instance.name = "ExternalMoaiVisual";
        instance.layer = original.gameObject.layer;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = Vector3.one * scale;
        instance.transform.localPosition = new Vector3(envelope.center.x - rotated.center.x * scale,
            envelope.center.y - rotated.center.y * scale, envelope.min.z - rotated.min.z * scale);
        original.GetComponent<MeshRenderer>().enabled = false;
        visualReplaced = true;
        return instance;
    }

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Moai);
		Destroy(minimapIcon);
		Destroy(alertIcon);
		Destroy(this);
		return true;
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "SHRINE_MOAI");
	}

	public override bool ShowInDebug()
	{
		return true;
	}

	public override string GetDebugName()
	{
		return debugName;
	}
}
