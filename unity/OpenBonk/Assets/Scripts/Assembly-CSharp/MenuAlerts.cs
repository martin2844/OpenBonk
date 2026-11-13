using UnityEngine;

public class MenuAlerts : MonoBehaviour
{
	public GameObject alertUnlocks;

	public GameObject alertQuests;

	public GameObject alertShop;

	private void Awake()
	{
		SaveManager.A_SavesLoaded += Refresh;
	}

	private void OnDestroy()
	{
		SaveManager.A_SavesLoaded -= Refresh;
	}

	private void OnEnable()
	{
		Refresh();
	}

	private void Refresh()
	{
		var progression = SaveManager.Instance != null ? SaveManager.Instance.progression : null;
		if (progression == null || progression.menuMeta == null) return;
		var menu = progression.menuMeta;
		if (alertUnlocks != null) alertUnlocks.SetActive((progression.newUnlockables != null && progression.newUnlockables.Count > 0) || !menu.hasVisitedUnlocks);
		if (alertQuests != null) alertQuests.SetActive(progression.HasUnclaimedQuests() || !menu.hasVisitedQuests);
		if (alertShop != null) alertShop.SetActive((progression.newShopItems != null && progression.newShopItems.Count > 0) || !menu.hasVisitedShop);
	}
}
