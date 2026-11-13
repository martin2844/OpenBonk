using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;
using UnityEngine.UI;

public class InteractableMicrowave : BaseInteractable
{
	[CompilerGenerated]
	private sealed class _003CCookItem_003Ed__36 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public InteractableMicrowave _003C_003E4__this;

		public EItem itemToCreate;

		private float _003Ctimer_003E5__2;

		private float _003CcookTime_003E5__3;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CCookItem_003Ed__36(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public Material matCommon;

	public Material matRare;

	public Material matEpic;

	public Material matLegendary;

	public Renderer meshRenderer;

	public static InteractableMicrowave currentlyInteracting;

	public Animator animator;

	public AudioSource sfxStart;

	public AudioSource sfxFinish;

	public GameObject particles;

	public GameObject explosion;

	public RawImage itemIcon;

	public Transform microwaveCenterTransform;

	public GameObject exclamationMark;

	public GameObject progressBar;

	public GameObject minimapIcon;

	public GameObject cookingParticles;

	public RawImage progressBarProgress;

	public static Action<EItem> A_Used;

	public static Action A_Exploded;

	private float readyAtTime;

	private bool hasItem;

	private EItem newItem;

	public static string debugName = "Microwaves";

	public EItemRarity rarity { get; private set; }

	public int usesLeft { get; private set; }

	public bool isCooking { get; private set; }

	private new void Start()
	{
		base.Start();
		rarity = Rarity.GetShadyGuyRarity(0f, new[] { .75f, .15f, .075f, .025f });
		if (meshRenderer != null)
		{
			switch (rarity)
			{
				case EItemRarity.Common: meshRenderer.sharedMaterial = matCommon; break;
				case EItemRarity.Rare: meshRenderer.sharedMaterial = matRare; break;
				case EItemRarity.Epic: meshRenderer.sharedMaterial = matEpic; break;
				case EItemRarity.Legendary: meshRenderer.sharedMaterial = matLegendary; break;
			}
		}
		usesLeft = GetUses(rarity);
	}

	public override bool Interact()
	{
		if (isCooking) return false;
		var player = MyPlayer.Instance;
		var inventory = player != null ? player.inventory : null;
		if (inventory == null || inventory.itemInventory == null) return false;
		if (hasItem)
		{
			hasItem = false;
			readyAtTime = MyTime.time + .5f;
			inventory.itemInventory.AddItem(newItem);
			if (animator != null) animator.Play("Idle");
			if (particles != null) particles.SetActive(false);
			if (usesLeft <= 0) Invoke(nameof(Explode), .5f);
			return true;
		}
		if (inventory.gold < GetPrice())
		{
			ShowError(LocalizationUtility.GetLocalizedString("PopupText", "CANT_AFFORD"));
			return false;
		}
		if (inventory.itemInventory.GetUniqueItemsInRarity(rarity) > 1)
		{
			if (UiManager.Instance == null || UiManager.Instance.encounterWindows == null) return false;
			currentlyInteracting = this;
			UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Microwave);
			return false;
		}
		var args = new Dictionary<string, string> { ["rarity"] = LocalizationUtility.GetRarity(rarity) };
		ShowError(LocalizationUtility.GetLocalizedString("PopupText", "MICROWAVE_NEED_RARITY", args));
		return false;
	}

	private static void ShowError(string message)
	{
		var popup = AlwaysUi.Instance != null ? AlwaysUi.Instance.UiTextPopup : null;
		if (popup != null) popup.SetText(message, new Vector3(Screen.width * .55f, Screen.height * .55f, 0f), MyColorUtility.interactDisabledOutlineColor, .75f);
	}

	public void UseMicrowave(EItem eItemToCreate)
	{
		if (isCooking || hasItem || usesLeft <= 0) return;
		StartCoroutine(CookItem(eItemToCreate));
		if (isCooking) A_Used?.Invoke(eItemToCreate);
	}

	[IteratorStateMachine(typeof(_003CCookItem_003Ed__36))]
	private IEnumerator CookItem(EItem itemToCreate)
	{
		var player = MyPlayer.Instance;
		var inventory = player != null && player.inventory != null ? player.inventory.itemInventory : null;
		if (inventory == null || DataManager.Instance == null) yield break;
		var candidates = new List<EItem>();
		foreach (EItem item in inventory.items.Keys)
			if (item != itemToCreate && DataManager.Instance.GetItem(item).rarity == rarity) candidates.Add(item);
		if (candidates.Count == 0) yield break;
		usesLeft--;
		EItem consumed = candidates[global::Utility.MyRandom.random.Next(0, candidates.Count)];
		inventory.RemoveItem(consumed, false);
		if (EffectManager.Instance != null && EffectManager.Instance.stealItemWui != null && microwaveCenterTransform != null)
			EffectManager.Instance.TakeItem(DataManager.Instance.GetItem(consumed), microwaveCenterTransform, Vector3.zero, .85f, .15f, 1f);
		player.inventory.ChangeGold(-GetPrice());
		isCooking = true;
		if (sfxStart != null) sfxStart.Play();
		if (animator != null) animator.Play("Start");
		if (exclamationMark != null) exclamationMark.SetActive(false);
		if (cookingParticles != null) cookingParticles.SetActive(true);
		if (progressBar != null) progressBar.SetActive(true);
		float timer = 0f, duration = GetCookTime();
		while (timer < duration)
		{
			timer += MyTime.deltaTime;
			if (progressBarProgress != null) progressBarProgress.transform.localScale = new Vector3(timer / duration, 1f, 1f);
			yield return null;
		}
		isCooking = false;
		hasItem = true;
		newItem = itemToCreate;
		if (sfxFinish != null) sfxFinish.Play();
		if (sfxStart != null) sfxStart.Stop();
		if (animator != null) animator.Play("Finish");
		if (itemIcon != null) itemIcon.texture = DataManager.Instance.GetItem(newItem).icon;
		if (particles != null) particles.SetActive(true);
		if (exclamationMark != null) exclamationMark.SetActive(true);
		if (cookingParticles != null) cookingParticles.SetActive(false);
		if (progressBar != null) progressBar.SetActive(false);
	}

	private float GetCookTime()
	{
		return 4f + 2f * (int)rarity;
	}

	private void Explode()
	{
		if (exclamationMark != null) exclamationMark.SetActive(false);
		if (explosion != null) explosion.SetActive(true);
		if (animator != null) animator.Play("Explode");
		if (minimapIcon != null) minimapIcon.SetActive(false);
		A_Exploded?.Invoke();
	}

	public override bool CanInteract()
	{
		return hasItem || (usesLeft > 0 && !isCooking && MyTime.time > readyAtTime);
	}

	public override string GetInteractString()
	{
		if (hasItem)
		{
			var args = new Dictionary<string, string> { ["item"] = EnumUtility.EnumToReadable(newItem.ToString()) };
			return LocalizationUtility.GetLocalizedString("Game_Interactables", "MICROWAVE_TAKE", args);
		}
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "MICROWAVE_USE") + string.Format(" | <sprite name=gold>{0}", GetPrice());
	}

	public int GetPrice()
	{
		return Mathf.RoundToInt(Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice() * .34f);
	}

	private int GetUses(EItemRarity itemRarity)
	{
		switch (itemRarity)
		{
			case EItemRarity.Rare:
			case EItemRarity.Epic: return 2;
			case EItemRarity.Legendary: return 1;
			default: return 3;
		}
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
