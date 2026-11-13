using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChestItem : MonoBehaviour
{
	public RawImage icon;

	public TextMeshProUGUI t_amountText;

	public AudioClip clipPositive;

	public AudioClip clipNegative;

	public AudioSource audioSource;

	public void Set(ItemData itemData, int amount)
	{
		icon.texture = itemData.icon;
		t_amountText.color = amount < 0 ? Color.red : Color.white;
		t_amountText.text = amount.ToString();
		audioSource.clip = amount > 0 ? clipPositive : clipNegative;
	}
}
