using TMPro;
using UnityEngine;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class ShopFooter : MonoBehaviour
{
	public TextMeshProUGUI t_title;

	public TextMeshProUGUI t_description;

	public ShopContainer shopContainer;

	public RequirementsContainer requirementsContainer;

	public GameObject buy;

	public GameObject refund;

	public void Set(ShopContainer shopContainerClicked)
	{
		if (shopContainerClicked == null || shopContainerClicked.data == null) return;
		ShopItemData item = shopContainerClicked.data;
		bool unlocked = MyAchievements.IsUnlocked(item, out _);
		if (t_description != null) t_description.gameObject.SetActive(unlocked);
		if (requirementsContainer != null) requirementsContainer.gameObject.SetActive(!unlocked);
		if (buy != null) buy.SetActive(unlocked);
		if (refund != null) refund.SetActive(unlocked);
		if (t_title != null) t_title.text = item.GetName();
		if (unlocked) SetUnlocked(item); else SetLocked(item);
	}

	private void SetLocked(ShopItemData shopItem)
	{
		if (shopContainer != null) shopContainer.Set(shopItem);
		if (requirementsContainer != null) requirementsContainer.Set(shopItem);
	}

	private void SetUnlocked(ShopItemData shopItem)
	{
		if (shopContainer != null) shopContainer.Set(shopItem);
		if (t_description != null) t_description.text = shopItem.GetDescription();
	}
}
