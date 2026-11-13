using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableShrineGreed : BaseInteractable
{
	public LocalizedString localizationName;

	public GameObject minimapIcon;

	public GameObject alertIcon;

	private bool done;

	public GameObject fx;

	public GameObject fxLoop;

	public EffectStat statEffect;

	public static string debugName = "Greed Shrines";

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		if (fx != null) fx.SetActive(true);
		if (fxLoop != null) fxLoop.SetActive(true);
		if (statEffect != null)
		{
			statEffect.ApplyEffect();
			string name = statEffect.GetEffectName();
			string number = statEffect.GetEffectNumber();
			if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(number) && UiManager.Instance != null && UiManager.Instance.scoreUi != null)
				UiManager.Instance.scoreUi.AddScore(name, number);
		}
		if (PoolManager.Instance != null && PoolManager.Instance.goldPool != null)
			MoneyUtility.SpawnMoney(Mathf.Max(25, (int)(MoneyUtility.GetChestPrice() * .1f)), transform.position);
		Destroy(minimapIcon);
		Destroy(alertIcon);
		base.OnDestroy();
		return true;
	}

	public override bool CanInteract()
	{
		return !done;
	}

	public override string GetInteractString()
	{
		return localizationName != null ? localizationName.GetLocalizedString() : "";
	}

	public override bool ShowInDebug()
	{
		return true;
	}

	public override string GetDebugName()
	{
		return debugName;
	}
}
