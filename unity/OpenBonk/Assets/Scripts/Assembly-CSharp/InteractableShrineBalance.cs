using UnityEngine;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;

public class InteractableShrineBalance : BaseInteractable
{
	public GameObject minimapIcon;

	public GameObject alertIcon;

	public bool done;

	public static string debugName = "Bald Heads";

    private bool visualReplaced;

    protected new void Start()
    {
        if (Assets.Scripts.Managers.MapController.currentMap is OpenBonkExternalMapData map &&
            Assets.Scripts.Managers.MapController.currentStage != null &&
            map.balanceVisuals.TryGetValue(Assets.Scripts.Managers.MapController.currentStage, out var visual))
            ReplaceVisual(visual);
        base.Start();
    }

    internal GameObject ReplaceVisual(GameObject visual)
    {
        if (visual == null || visualReplaced) return null;
        var instance = OpenBonkShrineVisual.Replace(this, visual, "ExternalBalanceVisual");
        if (instance != null) visualReplaced = true;
        return instance;
    }

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		UiManager.Instance.encounterWindows.AddEncounter(EEncounter.BalanceShrine);
		Destroy(minimapIcon);
		Destroy(alertIcon);
		Destroy(this);
		return true;
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "SHRINE_BALANCE");
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
