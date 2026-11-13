using UnityEngine;

public class SelectionGroupToggleSingleButton : MyButton
{
	public GameObject[] enableOnSelect;

	public GameObject unselectableOverlay;

	public bool canSelect { get; set; } = true;

	public bool isSelected { get; private set; }

	public void Select()
	{
		isSelected = true;
		if (enableOnSelect != null) foreach (GameObject obj in enableOnSelect) if (obj != null) obj.SetActive(true);
	}

	public void Deselect()
	{
		isSelected = false;
		if (enableOnSelect != null) foreach (GameObject obj in enableOnSelect) if (obj != null) obj.SetActive(false);
	}

	public override void StartHover()
	{
		isHovering = true;
	}

	public override void StopHover()
	{
		isHovering = false;
	}

	protected override void OnClick()
	{
	}

	public void CanSelect(bool b)
	{
		canSelect = b;
		if (unselectableOverlay != null) unselectableOverlay.SetActive(!b);
	}
}
