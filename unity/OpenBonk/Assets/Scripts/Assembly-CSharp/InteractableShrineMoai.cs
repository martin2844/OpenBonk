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
        var instance = OpenBonkShrineVisual.Replace(this, visual, "ExternalMoaiVisual");
        if (instance != null) visualReplaced = true;
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
