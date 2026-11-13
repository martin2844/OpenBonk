using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnlockContainer : MonoBehaviour, ISelectHandler, IEventSystemHandler
{
	public UnlockableBase unlockable;

	public RawImage icon;

	public RawImage fullReleaseOnly;

	public RawImage backgroundLocked;

	public RawImage backgroundUnlocked;

	public Texture t_unknown;

	public string requirementsString;

	public bool isUnlocked;

	public bool fullGameOnly;

	public bool isPurchased;

	public GameObject notPurchasedOverlay;

	public TextMeshProUGUI t_price;

	public GameObject alert;

	public GameObject activationToggle;

	public GameObject activationToggleCheckmark;

	public GameObject unactivatedOverlay;

	private Button button;

	public static Action<UnlockContainer> A_Selected;

	public static Action<UnlockContainer> A_Clicked;

	public Color defaultBackgroundColor;

	public bool visualsOnlyNoButton;

	public static Action A_RemovedAlert;

	private void Awake()
	{
		button = GetComponent<Button>();
		UnlocksFooter.A_Purchased += OnPurchased;
	}

	private void OnDestroy()
	{
		UnlocksFooter.A_Purchased -= OnPurchased;
	}

	private void OnEnable()
	{
		if (unlockable != null && SaveManager.Instance?.progression != null) Set(unlockable);
	}

	public void Set(UnlockableBase unlockable)
	{
		this.unlockable = unlockable;
		if (icon != null) icon.enabled = true;
		fullGameOnly = unlockable == null || !unlockable.isEnabled;
		isUnlocked = !fullGameOnly && MyAchievements.IsUnlocked(unlockable, out requirementsString);
		if (fullGameOnly) requirementsString = string.Empty;
		if (fullReleaseOnly != null) fullReleaseOnly.gameObject.SetActive(fullGameOnly);
		if (icon != null) icon.texture = fullGameOnly ? t_unknown : unlockable.GetIcon();
		if (backgroundUnlocked != null) backgroundUnlocked.color = GetBackgroundColor(unlockable);
		isPurchased = unlockable != null && SaveManager.Instance?.progression != null && MyAchievements.IsPurchased(unlockable);
		if (notPurchasedOverlay != null) notPurchasedOverlay.SetActive(isUnlocked && !isPurchased);
		if (t_price != null) t_price.text = unlockable == null ? string.Empty : $"<sprite name=silver> {unlockable.GetPrice()}";
		if (alert != null) alert.SetActive(unlockable != null && SaveManager.Instance?.progression?.newUnlockables != null && SaveManager.Instance.progression.newUnlockables.Contains(unlockable.GetInternalName()));
		SetUnlocked(isUnlocked);
		bool canToggle = isPurchased && MyAchievements.CanToggleActivation(unlockable);
		if (activationToggle != null) activationToggle.SetActive(canToggle);
		bool active = canToggle && MyAchievements.IsActivated(unlockable);
		if (activationToggleCheckmark != null) activationToggleCheckmark.SetActive(active);
		if (unactivatedOverlay != null) unactivatedOverlay.SetActive(canToggle && !active);
	}

	public void SetEmpty()
	{
		if (icon != null) icon.enabled = false;
		if (notPurchasedOverlay != null) notPurchasedOverlay.SetActive(false);
	}

	public void SetUnlocked(bool isUnlocked)
	{
		if (icon != null) icon.color = isUnlocked ? Color.white : Color.black;
		if (backgroundLocked != null) backgroundLocked.gameObject.SetActive(!isUnlocked);
		if (backgroundUnlocked != null) backgroundUnlocked.gameObject.SetActive(isUnlocked);
	}

	public void SetAchievement(MyAchievement ach)
	{
		isUnlocked = ach != null && MyAchievements.IsUnlocked(ach);
		SetUnlocked(isUnlocked);
		if (icon != null)
		{
			icon.enabled = true;
			icon.texture = ach != null ? ach.GetIcon() : t_unknown;
		}
	}

	private Color GetBackgroundColor(UnlockableBase unlockable)
	{
		return unlockable is ItemData item ? item.GetColor() : defaultBackgroundColor;
	}

	public void OnSelect(BaseEventData eventData)
	{
		if (unlockable != null && SaveManager.Instance?.progression?.newUnlockables != null && SaveManager.Instance.progression.newUnlockables.Remove(unlockable.GetInternalName()))
		{
			if (alert != null) alert.SetActive(false);
			A_RemovedAlert?.Invoke();
		}
		A_Selected?.Invoke(this);
	}

	public void ToggleActivation()
	{
		if (unlockable == null || !MyAchievements.IsPurchased(unlockable) || !MyAchievements.CanToggleActivation(unlockable)) return;
		bool active = MyAchievements.IsActivated(unlockable);
		if (activationToggleCheckmark != null) activationToggleCheckmark.SetActive(!active);
		if (unactivatedOverlay != null) unactivatedOverlay.SetActive(active);
		if (active) SaveManager.Instance.progression.inactivated.Add(unlockable.GetInternalName());
		else SaveManager.Instance.progression.inactivated.Remove(unlockable.GetInternalName());
	}

	private void OnPurchased(UnlockableBase unlockable)
	{
		if (this.unlockable == unlockable) Set(unlockable);
	}
}
