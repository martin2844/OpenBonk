using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

public class XpAndGoldHUD : MonoBehaviour
{
	public XpAndGoldText xpText;

	public XpAndGoldText goldText;

	public XpAndGoldText silverText;

	private void Awake()
	{
		PlayerInventory.A_GoldChange += OnGoldIncrease;
		PlayerXp.A_XpAdded += OnXpIncrease;
		ProgressionSaveFile.A_SilverChanged += OnSilverChange;
	}

	private void OnDestroy()
	{
		PlayerInventory.A_GoldChange -= OnGoldIncrease;
		PlayerXp.A_XpAdded -= OnXpIncrease;
		ProgressionSaveFile.A_SilverChanged -= OnSilverChange;
	}

	private void OnGoldIncrease(PlayerInventory inv, int amount)
	{
		if (IsActive() && goldText != null) goldText.Add(amount);
	}

	private void OnXpIncrease(PlayerXp pXp, int amount)
	{
		if (IsActive() && xpText != null) xpText.Add(amount);
	}

	private void OnSilverChange(int amount)
	{
		if (IsActive() && silverText != null) silverText.Add(amount);
	}

	private bool IsActive()
	{
		return SaveManager.Instance?.config?.cfVisualsSettings != null &&
			SaveManager.Instance.config.cfVisualsSettings.xp_gold_hud == 1;
	}
}
