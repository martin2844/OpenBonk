using Inventory__Items__Pickups.Xp_and_Levels;
using System.Collections.Generic;
using Assets.Scripts.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;
using Inventory__Items__Pickups;

public class XpBarUI : MonoBehaviour
{
	public TextMeshProUGUI t_levelText;

	public RawImage xpBar;

	private Color defaultColor;

	public Material rainbow;

	private Material defaultMaterial;

	private float desiredRatio;

	private void Awake()
	{
		SetLevelText(0);
		if (xpBar != null) { defaultColor = xpBar.color; defaultMaterial = xpBar.material; }
		PlayerXp.A_XpAdded += OnXpIncrease;
		PlayerXp.A_LevelUp += OnLevelUp;
		LevelupScreen.A_LevelupEnabled += OnLevelupShow;
		LevelupScreen.A_LevelUpClose += OnLevelupHide;
		MyPlayer.A_PlayerInventoryInitialized += OnInventoryInitialized;
	}

	private void OnDestroy()
	{
		PlayerXp.A_XpAdded -= OnXpIncrease;
		PlayerXp.A_LevelUp -= OnLevelUp;
		LevelupScreen.A_LevelupEnabled -= OnLevelupShow;
		LevelupScreen.A_LevelUpClose -= OnLevelupHide;
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInitialized;
	}

	private void Start()
	{
		if (GameManager.Instance != null && GameManager.Instance.GetPlayerInventory() != null)
			Refresh(GameManager.Instance.GetPlayerInventory().playerXp);
	}

	private void Refresh(PlayerXp playerXp)
	{
		if (playerXp == null) return;
		OnXpIncrease(playerXp, 0);
		SetLevelText(playerXp.level);
	}

	private void SetLevelText(int level)
	{
		if (t_levelText == null) return;
		try {
			string value = LocalizationUtility.GetLocalizedString("Game_HUD", "LEVEL", new Dictionary<string, string> { ["level"] = level.ToString() });
			t_levelText.text = string.IsNullOrEmpty(value) ? $"Level {level}" : value;
		} catch (System.Exception) { t_levelText.text = $"Level {level}"; }
	}

	private void OnInventoryInitialized(PlayerInventory pInventory)
	{
		Refresh(pInventory.playerXp);
	}

	private void OnXpIncrease(PlayerXp pXp, int amount)
	{
		if (pXp != null) desiredRatio = XpUtility.CurrentLevelProgress(pXp.GetXpInt());
	}

	private void Update()
	{
		if (xpBar == null) return;
		Vector3 target = new Vector3(LevelupScreen.isLevelingUp ? 1f : desiredRatio, 1f, 1f);
		xpBar.transform.localScale = Vector3.Lerp(xpBar.transform.localScale, target, Time.deltaTime * 8f);
	}

	private void OnLevelUp(int level)
	{
		SetLevelText(level);
	}

	private void OnLevelupShow()
	{
		if (xpBar != null) xpBar.material = rainbow;
	}

	private void OnLevelupHide()
	{
		if (xpBar == null) return;
		xpBar.material = defaultMaterial;
		xpBar.transform.localScale = new Vector3(0f, 1f, 1f);
	}
}
