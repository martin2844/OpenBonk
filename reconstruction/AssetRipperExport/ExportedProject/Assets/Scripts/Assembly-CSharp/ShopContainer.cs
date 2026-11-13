using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class ShopContainer : MonoBehaviour
{
	public RawImage icon;

	public Transform levelsParent;

	public GameObject backgroundLocked;

	public GameObject backgroundUnlocked;

	public GameObject alert;

	public ShopItemData data { get; private set; }

	private void Awake()
	{
		ShopWindow.A_LevelChanged += OnShopItemLevelChanged;
		MyButtonShop.A_Select += OnButtonSelect;
	}

	private void OnButtonSelect(ShopContainer shopBtn)
	{
		if (shopBtn != this || data == null || SaveManager.Instance?.progression?.newShopItems == null) return;
		if (SaveManager.Instance.progression.newShopItems.Remove(data.GetInternalName()) && alert != null) alert.SetActive(false);
	}

	private void OnDestroy()
	{
		ShopWindow.A_LevelChanged -= OnShopItemLevelChanged;
		MyButtonShop.A_Select -= OnButtonSelect;
	}

	public void Set(ShopItemData shopItemData)
	{
		data = shopItemData;
		if (data == null || SaveManager.Instance?.progression == null) return;
		if (icon != null) icon.texture = data.GetIcon();
		bool unlocked = MyAchievements.IsUnlocked(data, out _);
		if (icon != null) icon.color = unlocked ? Color.white : Color.black;
		if (backgroundLocked != null) backgroundLocked.SetActive(!unlocked);
		if (backgroundUnlocked != null) backgroundUnlocked.SetActive(unlocked);
		if (alert != null) alert.SetActive(SaveManager.Instance.progression.newShopItems != null && SaveManager.Instance.progression.newShopItems.Contains(data.GetInternalName()));
		RefreshLevel(unlocked);
	}

	private void Update()
	{
	}

	private void RefreshLevel(bool isUnlocked)
	{
		if (levelsParent == null || data == null) return;
		int max = data.GetMaxLevel(), level = data.GetLevel();
		for (int i = 0; i < levelsParent.childCount; i++)
		{
			Transform child = levelsParent.GetChild(i);
			child.gameObject.SetActive(isUnlocked && i < max);
			if (child.childCount > 0) child.GetChild(0).gameObject.SetActive(i < level);
		}
	}

	private void OnShopItemLevelChanged(ShopContainer shopContainer)
	{
		if (shopContainer != null && shopContainer.data == data && data != null) RefreshLevel(MyAchievements.IsUnlocked(data, out _));
	}
}
