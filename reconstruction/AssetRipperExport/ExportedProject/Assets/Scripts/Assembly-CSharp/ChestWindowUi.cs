using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.UI.InGame.Rewards;
using TMPro;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class ChestWindowUi : BaseEncounterWindow
{
	[CompilerGenerated]
	private sealed class _003CAnimateSingleTextObject_003Ed__22 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public TextMeshProUGUI text;

		public float fadeTime;

		private float _003Ctimer_003E5__2;

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
		public _003CAnimateSingleTextObject_003Ed__22(int _003C_003E1__state)
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

	[CompilerGenerated]
	private sealed class _003CAnimateText_003Ed__21 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public ChestWindowUi _003C_003E4__this;

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
		public _003CAnimateText_003Ed__21(int _003C_003E1__state)
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

	public TextMeshProUGUI t_itemName;

	public TextMeshProUGUI t_itemDesc;

	public TextMeshProUGUI t_itemRarity;

	public ChestOpening chestOpening;

	public LevelupScreen levelupScreen;

	public MyButtonOffersUtility b_banish;

	public MyButton b_open;

	public MyButton b_leave;

	public MyButton b_take;

	public ItemData itemData;

	public Window window;

	public static Action A_Open;

	public static Action A_Close;

	private EChest chestType;
	private Vector2 descriptionAnchor;
	private bool descriptionAnchorSaved;

	private void Awake()
	{
		ChestOpening.A_ChestFinished += OpeningFinished;
	}

	private void OnDestroy()
	{
		ChestOpening.A_ChestFinished -= OpeningFinished;
	}

	public override void Open(EEncounter encounterType)
	{
		chestOpening.gameObject.SetActive(true);
		gameObject.SetActive(true);
		chestType = ChestUtility.EncounterToChestType(encounterType);
		chestOpening.SetChest(chestType);
		b_take.gameObject.SetActive(false);
		b_leave.gameObject.SetActive(false);
		b_open.gameObject.SetActive(false);
		b_banish.gameObject.SetActive(false);
		t_itemName.CrossFadeAlpha(0f, 0f, true);
		t_itemDesc.CrossFadeAlpha(0f, 0f, true);
		t_itemRarity.CrossFadeAlpha(0f, 0f, true);
		Invoke(nameof(ShowOpenButton), 0.35f);
		A_Open?.Invoke();
	}

	private void ShowOpenButton()
	{
		b_open.gameObject.SetActive(true);
	}

	public override void OnClose()
	{
		gameObject.SetActive(false);
		MyInputManager.RefreshHorizontalNavigationForChests(false);
		A_Close?.Invoke();
	}

	public override void ChooseOffer(int index)
	{
	}

	private void OpeningFinished(ItemData unused)
	{
		b_take.gameObject.SetActive(true);
		b_banish.gameObject.SetActive(true);
		b_banish.SetAmount(levelupScreen.GetBanishes(), levelupScreen.GetShopToolPrice(levelupScreen.GetBanishesUsed()));
		Assets.Scripts.Managers.ButtonManager.ForceHoverButton(b_take);
		ControllerShaker.StopShakes();
		ControllerShaker.Shake(0, .7f, .25f);
		StartCoroutine(AnimateText());
	}

	[IteratorStateMachine(typeof(_003CAnimateText_003Ed__21))]
	private IEnumerator AnimateText()
	{
		t_itemName.text = itemData.GetName();
		t_itemDesc.text = itemData.GetDescription();
		t_itemRarity.text = LocalizationUtility.GetRarity(itemData.rarity);
		t_itemRarity.color = MyColorUtility.GetItemRarityColor(itemData.rarity);
		PositionDescription();
		StartCoroutine(AnimateSingleTextObject(t_itemRarity, .35f));
		StartCoroutine(AnimateSingleTextObject(t_itemName, .35f));
		yield return new WaitForSeconds(.25f);
		yield return StartCoroutine(AnimateSingleTextObject(t_itemDesc, .35f));
	}

	private void PositionDescription()
	{
		if (t_itemName == null || t_itemDesc == null || t_itemDesc.transform.parent == null) return;
		var rect = t_itemDesc.rectTransform;
		if (!descriptionAnchorSaved) { descriptionAnchor = rect.anchoredPosition; descriptionAnchorSaved = true; }
		rect.anchoredPosition = descriptionAnchor;
		t_itemName.ForceMeshUpdate();
		t_itemDesc.ForceMeshUpdate();
		Transform parent = rect.parent;
		float nameBottom = float.PositiveInfinity, descriptionTop = float.NegativeInfinity;
		for (int i = 0; i < t_itemName.textInfo.characterCount; i++)
		{
			var ch = t_itemName.textInfo.characterInfo[i];
			if (ch.isVisible) nameBottom = Mathf.Min(nameBottom, parent.InverseTransformPoint(t_itemName.transform.TransformPoint(ch.bottomLeft)).y);
		}
		for (int i = 0; i < t_itemDesc.textInfo.characterCount; i++)
		{
			var ch = t_itemDesc.textInfo.characterInfo[i];
			if (ch.isVisible) descriptionTop = Mathf.Max(descriptionTop, parent.InverseTransformPoint(t_itemDesc.transform.TransformPoint(ch.topRight)).y);
		}
		if (float.IsInfinity(nameBottom) || float.IsInfinity(descriptionTop)) return;
		float shift = Mathf.Max(0f, descriptionTop - nameBottom + 4f);
		rect.anchoredPosition = descriptionAnchor - Vector2.up * shift;
	}

	[IteratorStateMachine(typeof(_003CAnimateSingleTextObject_003Ed__22))]
	private IEnumerator AnimateSingleTextObject(TextMeshProUGUI text, float fadeTime)
	{
		yield return null;
		text.CrossFadeAlpha(1f, fadeTime, true);
		float timer = 0f;
		while (timer < fadeTime)
		{
			timer += Time.deltaTime;
			text.transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, Easing.InCirc(Mathf.Clamp01(timer / fadeTime)));
			yield return null;
		}
		text.transform.localScale = Vector3.one;
	}

	public void OpenButton()
	{
		try { itemData = ItemUtility.GetRandomChestItem(chestType, PlayerStats.GetStat(EStat.Luck)); }
		catch (Exception e) { UnityEngine.Debug.LogError(e.Message); itemData = DataManager.Instance.unsortedItems[0]; }
		chestOpening.OpenChest(itemData);
		b_open.gameObject.SetActive(false);
		MyInputManager.RefreshHorizontalNavigationForChests(true);
	}

	public void TakeButton()
	{
		MyPlayer.Instance.inventory.itemInventory.AddItem(itemData.eItem);
		UiManager.Instance.encounterWindows.RewardFinished();
	}

	public void DiscardButton()
	{
		var player = MyPlayer.Instance.transform;
		Instantiate(EffectManager.Instance.chestDiscard, player.position + player.up * 1.5f, Quaternion.identity);
		UiManager.Instance.encounterWindows.RewardFinished();
	}

	public void BanishButton()
	{
		int price = levelupScreen.GetShopToolPrice(levelupScreen.GetBanishesUsed());
		if (levelupScreen.GetBanishes() <= 0 || MyPlayer.Instance.inventory.gold < price) return;
		MyPlayer.Instance.inventory.ChangeGold(-price);
		Assets.Scripts.Inventory__Items__Pickups.RunUnlockables.BanishItem(itemData);
		levelupScreen.DecrementBanishes();
		UiManager.Instance.encounterWindows.RewardFinished();
		EffectManager.Instance.BanishItem(itemData);
	}
}
