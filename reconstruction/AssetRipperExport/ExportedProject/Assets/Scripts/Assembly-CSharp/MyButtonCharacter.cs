using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonCharacter : MyButton
{
	public GameObject hoverOverlay;

	public GameObject unavailableOverlay;

	public TextMeshProUGUI t_name;

	public RawImage i_icon;

	public GameObject requiresPurchaseOverlay;

	public TextMeshProUGUI t_price;

	public GameObject star;

	public CharacterData characterData;

	public Material lockedMaterial;

	public static Action<MyButtonCharacter> A_Confirm;

	public static Action<MyButtonCharacter> A_Select;

	public bool canUseCharacter;

	private CharacterData data;

	public string cantUseCharacterReason;

	private new void Awake()
	{
		UnlocksFooter.A_Purchased += OnItemPurchased;
	}

	private void OnEnable()
	{
		if (data != null && t_name != null) t_name.text = data.GetName();
	}

	private void OnDestroy()
	{
		UnlocksFooter.A_Purchased -= OnItemPurchased;
	}

	private void OnItemPurchased(UnlockableBase unlockable)
	{
		SetCharacter(data);
	}

	public void SetCharacter(CharacterData data)
	{
		this.data = data;
		canUseCharacter = false;
		if (data == null || !data.isEnabled)
		{
			characterData = null;
			if (t_name != null) t_name.text = "???";
			if (i_icon != null && Assets.Scripts.Managers.IconManager.Instance != null) i_icon.texture = Assets.Scripts.Managers.IconManager.Instance.questionMark;
			cantUseCharacterReason = "Character not enabled..";
			return;
		}
		characterData = data;
		if (t_name != null) t_name.text = data.GetName();
		if (i_icon != null) i_icon.texture = data.GetIcon();
		if (star != null && SaveManager.Instance != null && SaveManager.Instance.progression != null)
			star.SetActive(SaveManager.Instance.progression.GetCharacterProgression(data.eCharacter).HasStar());
		bool unlocked = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(data, out _);
		if (i_icon != null) i_icon.color = unlocked ? Color.white : Color.black;
		if (!unlocked)
		{
			cantUseCharacterReason = Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("PopupText", "CHARACTER_NOT_UNLOCKED");
			if (data.IsBlackedOutInCharacterSelectionScreen())
			{
				if (unavailableOverlay != null) unavailableOverlay.SetActive(true);
				if (t_name != null) t_name.text = "??";
			}
			return;
		}
		canUseCharacter = true;
		bool purchased = Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsPurchased(data);
		if (requiresPurchaseOverlay != null) requiresPurchaseOverlay.SetActive(!purchased);
		if (!purchased && t_price != null) t_price.text = string.Format("<size=110%><sprite name=silver></size> {0:N0}", data.GetPrice());
	}

	public void Refresh()
	{
		SetCharacter(data);
	}

	public override void StartHover()
	{
		isHovering = true;
		if (hoverOverlay != null) hoverOverlay.SetActive(true);
		A_Select?.Invoke(this);
	}

	public override void StopHover()
	{
		isHovering = false;
		if (hoverOverlay != null) hoverOverlay.SetActive(false);
	}

	protected override void OnClick()
	{
	}

	private new void Update()
	{
		base.Update();
		if (isHovering && MyInputManager.GetButtonDown(MyInputManager.UISubmit) && Assets.Scripts.Managers.ButtonManager.GetCurrentButton() == this)
		{
			PlaySfx();
			A_Confirm?.Invoke(this);
		}
	}
}
