using UnityEngine;
using UnityEngine.Localization;

public class InteractableBoombox : BaseInteractable
{
	public LocalizedString interactString;

	public GameObject alertIcon;

	private bool done;

	public GameObject fx;

	public AudioSource music;

	public Animator animator;

	public MusicPauseZone pauseZone;

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		fx.SetActive(true);
		music.Play();
		animator.Play("Play");
		Destroy(alertIcon);
		pauseZone.enabled = true;
		return true;
	}

	public override bool CanInteract()
	{
		return !done;
	}

	public override string GetInteractString()
	{
		return interactString.GetLocalizedString();
	}
}
