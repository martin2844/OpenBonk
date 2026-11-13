using UnityEngine;
using UnityEngine.UI;

public interface IButton
{
	void Select();

	void Deselect(IButton newButton);

	Button GetButton();

	GameObject GetAssociatedContent();

	bool IsTabSelectable();

	void Enable();

	void Disable();
}
