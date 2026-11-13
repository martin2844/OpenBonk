using TMPro;
using UnityEngine;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;

public class ShrineInformationPrefab : MonoBehaviour
{
	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_values;

	private string debugName;

	private InteractablesStatus.InteractableStatusContainer container;

	public void Set(InteractablesStatus.InteractableStatusContainer container, string name)
	{
		this.debugName = name;
		this.container = container;
		if (t_name != null) t_name.text = name;
		Refresh();
	}

	public void Refresh()
	{
		bool show = CanShow();
		gameObject.SetActive(show);
		if (show && container != null) SetValue(container.numUsed, container.numTotal);
	}

	public void SetValue(int current, int max)
	{
		if (t_values != null) t_values.text = $"{current} / {max}";
	}

	public bool CanShow()
	{
		bool crypt = GameManager.Instance != null && GameManager.Instance.isCrypt;
		bool cryptEntry = debugName == InteractableChest.debugNameCrypt || debugName == InteractablePot.debugNameCrypt;
		return crypt == cryptEntry;
	}
}
