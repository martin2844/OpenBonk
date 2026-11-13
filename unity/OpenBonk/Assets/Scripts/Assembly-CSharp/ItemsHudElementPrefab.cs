using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemsHudElementPrefab : MonoBehaviour
{
	public RawImage icon;

	public TextMeshProUGUI t_amount;

	public void Set(ItemData itemData)
	{
		icon.texture = itemData.GetIcon();
	}

	public void SetAmount(int amount)
	{
		t_amount.text = amount <= 1 ? string.Empty : "x" + amount;
	}
}
