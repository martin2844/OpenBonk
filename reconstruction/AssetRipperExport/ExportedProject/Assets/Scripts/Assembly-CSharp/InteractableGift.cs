using UnityEngine;
using UnityEngine.Localization;

public class InteractableGift : BaseInteractable
{
	public LocalizedString localizationOpenGift;

	public GameObject hatFloating;

	public GameObject fx;

	private bool done;

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		fx.transform.SetParent(null);
		hatFloating.transform.SetParent(null);
		fx.SetActive(true);
		hatFloating.SetActive(true);
		Destroy(gameObject);
		return true;
	}

	public override string GetInteractString()
	{
		return localizationOpenGift.GetLocalizedString();
	}
}
