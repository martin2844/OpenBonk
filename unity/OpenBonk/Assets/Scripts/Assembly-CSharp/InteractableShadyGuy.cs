using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;
using UnityEngine;

public class InteractableShadyGuy : BaseInteractable
{
	public Material matRare;

	public Material matEpic;

	public Material matLegendary;

	public SkinnedMeshRenderer meshRenderer;

	public GameObject smokeFx;

	public RandomSfx purchaseSfx;

	public GameObject[] hideAfterPurchase;

	public EItemRarity rarity;

	public List<ItemData> items;

	public List<int> prices;

	public static InteractableShadyGuy currentlyInteracting;

	private float[] pricesMultipliers;

	public static Action<InteractableShadyGuy> A_ShadyGuyDone;

	private bool done;

	public static string debugName = "Shady Guy";

	private new void Start()
	{
		base.Start();
		rarity = Rarity.GetShadyGuyRarity(0f);
		if (rarity == EItemRarity.Rare) meshRenderer.sharedMaterial = matRare;
		else if (rarity == EItemRarity.Epic) meshRenderer.sharedMaterial = matEpic;
		else if (rarity == EItemRarity.Legendary) meshRenderer.sharedMaterial = matLegendary;
		Invoke(nameof(FindItems), 1f);
		UpgradePicker.A_ShadyGuyDone += OnShadyGuyDone;
	}

	private void FindItems()
	{
		items = InventoryUtility.GetRandomItemsShadyGuy(rarity);
		pricesMultipliers = new float[items.Count];
		for (int i = 0; i < items.Count; i++) pricesMultipliers[i] = UnityEngine.Random.Range(0.5f, 1.5f);
		UpdatePrices();
	}

	private void UpdatePrices()
	{
		prices = new List<int>();
		for (int i = 0; i < items.Count; i++)
			prices.Add(Mathf.RoundToInt(MoneyUtility.GetItemPriceShadyGuy(items[i].rarity) * pricesMultipliers[i]));
	}

	private new void OnDestroy()
	{
		UpgradePicker.A_ShadyGuyDone -= OnShadyGuyDone;
		if (currentlyInteracting == this) currentlyInteracting = null;
		base.OnDestroy();
	}

	public override bool Interact()
	{
		if (done || items == null) return false;
		currentlyInteracting = this;
		UpdatePrices();
		UiManager.Instance.encounterWindows.AddEncounter(EEncounter.ShadyGuy);
		return false;
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "SHADY_GUY_INTERACT");
	}

	private void OnShadyGuyDone()
	{
		if (currentlyInteracting != this) return;
		done = true;
		if (purchaseSfx != null) purchaseSfx.Play();
		foreach (GameObject obj in hideAfterPurchase) if (obj != null) obj.SetActive(false);
		currentlyInteracting = null;
		A_ShadyGuyDone?.Invoke(this);
		Invoke(nameof(Disappear), 1f);
	}

	private void Disappear()
	{
		if (smokeFx != null)
		{
			smokeFx.transform.SetParent(null);
			smokeFx.SetActive(true);
			Destroy(smokeFx, 2f);
		}
		if (transform.parent != null) Destroy(transform.parent.gameObject);
		else Destroy(gameObject);
	}

	public override bool CanInteract()
	{
		return !done;
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
