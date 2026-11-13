using UnityEngine;
using UnityEngine.EventSystems;

public class UiFocusManager : MonoBehaviour
{
	private GameObject lastSelected;

	private void Update()
	{
		EventSystem eventSystem = EventSystem.current;
		if (eventSystem == null) return;
		if (eventSystem.currentSelectedGameObject != null)
			lastSelected = eventSystem.currentSelectedGameObject;
		else if (lastSelected != null && Input.GetMouseButtonDown(0))
			eventSystem.SetSelectedGameObject(lastSelected);
	}
}
