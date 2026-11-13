using System;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour
{
	public bool isItemSource;

	private Outline outline;

	protected DetectInteractables detectInteractables;

	public Vector3 textOffset;

	private Vector3 textOffsetCalculated;

	public static Action<string> A_DebugSpawn;

	public static Action<string> A_DebugDisable;

	public bool showOutline;

	protected void Start()
	{
		textOffsetCalculated = transform.right * textOffset.x + transform.up * textOffset.y + transform.forward * textOffset.z;
		if (!MapController.IsMainMenu() && isItemSource && ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS))
			transform.root.gameObject.SetActive(false);
		if (ShowInDebug()) A_DebugSpawn?.Invoke(GetDebugName());
	}

	public void StartHover(DetectInteractables detectInteractables)
	{
		if (outline != null) return;
		this.detectInteractables = detectInteractables;
		if (!showOutline) return;
		outline = gameObject.AddComponent<Outline>();
		outline.OutlineMode = Outline.Mode.OutlineVisible;
		outline.OutlineColor = GetColor();
		outline.OutlineWidth = 5f;
	}

	protected void RefreshInteractable()
	{
		if (detectInteractables == null) return;
		detectInteractables.RefreshCurrentInteractable();
		if (outline != null) outline.OutlineColor = GetColor();
	}

	public void StopHover()
	{
		detectInteractables = null;
		if (outline == null) return;
		Destroy(outline);
		outline = null;
	}

	protected void OnDestroy()
	{
		if (detectInteractables != null) detectInteractables.InteractableDestroyed();
	}

	public abstract bool Interact();

	public abstract string GetInteractString();

	public virtual Color GetColor()
	{
		return Color.white;
	}

	public virtual bool CanInteract()
	{
		return true;
	}

	public Vector3 GetOffset()
	{
		return textOffsetCalculated;
	}

	public virtual bool ShowInDebug()
	{
		return false;
	}

	public virtual string GetDebugName()
	{
		return string.Empty;
	}
}
