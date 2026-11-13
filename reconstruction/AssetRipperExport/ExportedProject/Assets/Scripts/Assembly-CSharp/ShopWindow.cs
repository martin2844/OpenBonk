using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class ShopWindow : MonoBehaviour
{
	public GameObject shopContainerPrefab;

	public Transform contentParent;

	public TabGridNavigation navigation;

	public Button btnBack;

	public ShopFooter shopFooter;

	public MyButtonNormal btnBuy;

	public MyButtonNormal btnRefund;

	public TextMeshProUGUI t_buy;

	public TextMeshProUGUI t_refund;

	private List<ShopContainer> shopContainers;

	public static Action<ShopContainer> A_LevelChanged;

	public ShopContainer currentContainer { get; private set; }

	private void Awake()
	{
		MyButtonShop.A_Select += OnShopSelect;
		MyButtonShop.A_Clicked += OnShopClicked;
	}

	private void OnDestroy()
	{
		MyButtonShop.A_Select -= OnShopSelect;
		MyButtonShop.A_Clicked -= OnShopClicked;
	}

	private void Start()
	{
		if (SaveManager.Instance?.progression?.menuMeta != null)
			SaveManager.Instance.progression.menuMeta.hasVisitedShop = true;
		if (DataManager.Instance == null || shopContainerPrefab == null) return;
		var items = new List<ShopItemData>(DataManager.Instance.shopItems.Values);
		items.Sort((a, b) => a.CompareTo(b));
		shopContainers = new List<ShopContainer>();
		for (int i = 0; i < items.Count; i++)
		{
			var obj = i == 0 ? shopContainerPrefab : Instantiate(shopContainerPrefab, contentParent);
			var container = obj.GetComponent<ShopContainer>();
			if (container == null) continue;
			shopContainers.Add(container);
			container.Set(items[i]);
		}
		if (navigation != null) navigation.Set(btnBack);
	}

	public void Buy()
	{
		var item = currentContainer != null ? currentContainer.data : null;
		if (item == null || item.IsMaxLevel() || !item.CanBuy()) return;
		if (SaveManager.Instance.progression.PurchaseShopItem(item) && AudioManager.Instance != null && AudioManager.Instance.purchaseSfx != null)
			AudioManager.Instance.purchaseSfx.Play();
		RefreshPrices();
		A_LevelChanged?.Invoke(currentContainer);
	}

	public void Refund()
	{
		var item = currentContainer != null ? currentContainer.data : null;
		if (item == null || !item.CanRefund()) return;
		if (SaveManager.Instance.progression.RefundShopItem(item) && AudioManager.Instance != null && AudioManager.Instance.purchaseSfx != null)
			AudioManager.Instance.purchaseSfx.Play();
		RefreshPrices();
		A_LevelChanged?.Invoke(currentContainer);
	}

	private void RefreshPrices()
	{
		var item = currentContainer != null ? currentContainer.data : null;
		if (item == null) return;
		bool max = item.IsMaxLevel(), refundable = item.CanRefund();
		if (t_buy != null) t_buy.text = max ? string.Empty : $"<sprite name=silver> {item.GetPrice()}";
		if (t_refund != null) t_refund.text = refundable ? $"<sprite name=silver> {item.GetRefundPrice()}" : string.Empty;
		if (btnBuy != null) btnBuy.SetDisabledOverlayButKeepInteractable(max);
		if (btnRefund != null) btnRefund.SetDisabledOverlayButKeepInteractable(!refundable);
	}

	private void OnShopClicked(ShopContainer shopContainerClicked)
	{
		if (shopContainerClicked == null || shopContainerClicked.data == null || !MyAchievements.IsUnlocked(shopContainerClicked.data, out _)) return;
		if (!shopContainerClicked.data.IsMaxLevel()) ButtonManager.ForceHoverButton(btnBuy);
		else if (shopContainerClicked.data.CanRefund()) ButtonManager.ForceHoverButton(btnRefund);
	}

	public void OnShopSelect(ShopContainer shopContainerClicked)
	{
		if (shopContainerClicked == null || shopContainerClicked.data == null) return;
		if (shopFooter != null) shopFooter.Set(shopContainerClicked);
		currentContainer = shopContainerClicked;
		RefreshPrices();
		if (btnRefund != null) btnRefund.gameObject.SetActive(shopContainerClicked.data.canRefund);
	}
}
