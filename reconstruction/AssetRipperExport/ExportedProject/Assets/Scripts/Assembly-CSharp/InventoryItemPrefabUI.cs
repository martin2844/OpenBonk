using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.EventSystems;
#endif
using Assets.Scripts._Data;

public class InventoryItemPrefabUI : MonoBehaviour
#if UNITY_ANDROID
	, ISelectHandler, IDeselectHandler
#endif
{
	public RawImage icon;

	public TextMeshProUGUI t_level;

	public GameObject lockedOverlay;

	public ToolTipObject toolTipObject;

	private UnlockableBase item;

#if UNITY_ANDROID
	private Button controllerButton;
	private GameObject controllerSelection;
#endif

	public GameObject banishedIcon;

	private void Awake()
	{
		WeaponInventory.A_WeaponToggled += OnWeaponToggled;
#if UNITY_ANDROID
		// This component is also used by encounter and death overlays. Their
		// navigation belongs to other windows, so only extend the pause menu.
		if (GetComponentInParent<PauseUi>(true) != null)
		{
			controllerButton = GetComponent<Button>();
			if (controllerButton == null) controllerButton = gameObject.AddComponent<Button>();
			controllerButton.transition = Selectable.Transition.None;
			controllerButton.interactable = item != null;
		}
#endif
	}

	private void OnDestroy()
	{
		WeaponInventory.A_WeaponToggled -= OnWeaponToggled;
	}

	private void OnWeaponToggled(WeaponBase weaponBase)
	{
		if (weaponBase != null && weaponBase.weaponData == item) RefreshEnabled(weaponBase.enabled);
	}

	public void SetBanished()
	{
		if (t_level != null) t_level.text = string.Empty;
		if (banishedIcon != null) banishedIcon.SetActive(true);
		if (icon != null) icon.color = new Color(0.5f, 0.5f, 0.5f, 1f);
	}

	public void SetItem(UnlockableBase item)
	{
		this.item = item;
#if UNITY_ANDROID
		if (controllerButton != null) controllerButton.interactable = item != null;
#endif
		if (lockedOverlay != null) lockedOverlay.SetActive(false);
		if (banishedIcon != null) banishedIcon.SetActive(false);
		if (icon != null) { icon.enabled = item != null; icon.color = Color.white; }
		if (item == null)
		{
			ClearTooltip();
			if (t_level != null) t_level.text = string.Empty;
			return;
		}
		if (icon != null) icon.texture = item is ItemData itemData ? itemData.icon : item.GetIcon();
		if (t_level != null) t_level.text = GetLevel(item).ToString();
		if (toolTipObject != null) toolTipObject.text = "<size=120%>" + item.GetName() + ":</size> " + item.GetDescription();
		if (item is WeaponData weaponData)
		{
			var player = Assets.Scripts.Actors.Player.MyPlayer.Instance;
			bool enabled = player == null || player.inventory == null || !player.inventory.weaponInventory.weapons.TryGetValue(weaponData.eWeapon, out var weapon) || weapon.enabled;
			RefreshEnabled(enabled);
		}
	}

	private void RefreshEnabled(bool isEnabled)
	{
		if (toolTipObject != null && item != null)
			toolTipObject.text = "<size=120%>" + item.GetName() + ":</size> " + item.GetDescription() + (isEnabled ? "" : " (Disabled)");
		if (icon != null) icon.color = isEnabled ? Color.white : Color.black;
	}

	public void SetItem(EItem eItem)
	{
		SetItem(DataManager.Instance.GetItem(eItem));
	}

	private int GetLevel(UnlockableBase item)
	{
		if (item is IUpgradable upgradable) return upgradable.GetLevel();
		if (item is ItemData itemData && GameManager.Instance != null)
			return GameManager.Instance.GetPlayerInventory().itemInventory.GetAmount(itemData.eItem);
		return 0;
	}

	public void SetUnavailable()
	{
		item = null;
#if UNITY_ANDROID
		if (controllerButton != null) controllerButton.interactable = false;
#endif
		ClearTooltip();
		if (lockedOverlay != null) lockedOverlay.SetActive(true);
		if (icon != null) icon.enabled = false;
		if (t_level != null) t_level.text = string.Empty;
	}

	private void ClearTooltip()
	{
		if (toolTipObject == null) return;
		toolTipObject.OnPointerExit(null);
		toolTipObject.text = string.Empty;
	}

#if UNITY_ANDROID
	public void OnSelect(BaseEventData eventData)
	{
		if (controllerButton == null) return;
		if (controllerSelection == null) CreateControllerSelection();
		if (controllerSelection != null) controllerSelection.SetActive(true);
		// Keep the game's separate hover state in sync without clearing EventSystem selection.
		Assets.Scripts.Managers.ButtonManager.StartedHoveringButton(null);
		if (SelectionArrow.Instance != null) SelectionArrow.Instance.Hide();
		if (item != null && toolTipObject != null && !string.IsNullOrWhiteSpace(toolTipObject.text) && ToolTip.Instance != null && transform is RectTransform rect)
			ToolTip.Instance.SetTip(toolTipObject.text, rect);
	}

	public void OnDeselect(BaseEventData eventData)
	{
		if (controllerButton == null) return;
		if (controllerSelection != null) controllerSelection.SetActive(false);
		if (ToolTip.Instance != null) ToolTip.Instance.HideTip();
	}

	private void OnDisable()
	{
		if (controllerSelection != null && controllerSelection.activeSelf)
		{
			controllerSelection.SetActive(false);
			if (ToolTip.Instance != null) ToolTip.Instance.HideTip();
		}
	}

	private void CreateControllerSelection()
	{
		if (!(transform is RectTransform)) return;
		controllerSelection = new GameObject("ControllerSelection", typeof(RectTransform));
		var border = (RectTransform)controllerSelection.transform;
		border.SetParent(transform, false);
		border.anchorMin = Vector2.zero;
		border.anchorMax = Vector2.one;
		border.offsetMin = Vector2.zero;
		border.offsetMax = Vector2.zero;
		for (int i = 0; i < 4; i++)
		{
			var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));
			var rect = (RectTransform)edge.transform;
			rect.SetParent(border, false);
			bool horizontal = i < 2;
			rect.anchorMin = horizontal ? new Vector2(0f, i) : new Vector2(i - 2, 0f);
			rect.anchorMax = horizontal ? new Vector2(1f, i) : new Vector2(i - 2, 1f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = horizontal ? new Vector2(0f, 3f) : new Vector2(3f, 0f);
			rect.anchoredPosition = Vector2.zero;
			var image = edge.GetComponent<Image>();
			image.color = Color.white;
			image.raycastTarget = false;
		}
	}
#endif
}
