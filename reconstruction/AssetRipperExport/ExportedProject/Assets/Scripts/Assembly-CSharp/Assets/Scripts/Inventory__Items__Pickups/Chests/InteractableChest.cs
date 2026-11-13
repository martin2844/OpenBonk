using System;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Chests
{
	public class InteractableChest : BaseInteractable
	{
		public EChest chestType;

		private float rotation;

		public Transform icon;

		public static Action A_ChestBought;

		public static Action A_ChestOpened;

		private bool opening;

		private bool isHoveringAndCantAfford;

		public bool isInCrypt;

		public bool isShownInDebug;

		public static string debugName = "Chests";

		public static string debugNameCrypt = "Crypt Chests";

		private void Awake()
		{
			ChestWindowUi.A_Close += OnChestWindowClose;
		}

		private new void OnDestroy()
		{
			ChestWindowUi.A_Close -= OnChestWindowClose;
			base.OnDestroy();
		}

		private new void Start()
		{
			base.Start();
			if (ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS)) gameObject.SetActive(false);
		}

		public override bool Interact()
		{
			if (!CanAfford())
			{
				if (AlwaysUi.Instance != null && AlwaysUi.Instance.UiTextPopup != null)
					AlwaysUi.Instance.UiTextPopup.SetText(Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("PopupText", "CANT_AFFORD"),
						new Vector3(Screen.width * .55f, Screen.height * .55f, 0f), Assets.Scripts.Utility.MyColorUtility.interactDisabledOutlineColor, .75f);
				return false;
			}
			opening = true;
			OpenChestImplementation();
			if (chestType != EChest.Free && chestType != EChest.FreeCrypt) A_ChestBought?.Invoke();
			A_ChestOpened?.Invoke();
			return true;
		}

		private void OpenChestImplementation()
		{
			if (SaveManager.Instance.config.cfGameSettings.skip_chest_animation == 1)
			{
				ChestUtility.OpenChestNoAnimation(chestType);
				OnChestWindowClose();
			}
			else UiManager.Instance.encounterWindows.AddEncounter(ChestUtility.ChestTypeToEncounter(chestType));
		}

		private void OnChestWindowClose()
		{
			if (!opening) return;
			var inventory = MyPlayer.Instance.inventory;
			if (inventory.itemInventory.GetItem(EItem.Key) is ItemKey key && key.GetChance() > global::Utility.MyRandom.random.NextDouble())
			{
				if (EffectManager.Instance != null && EffectManager.Instance.wuiFreeChest != null)
					Instantiate(EffectManager.Instance.wuiFreeChest, transform.position + Vector3.up * 1.5f, Quaternion.identity);
			}
			else
			{
				inventory.ChangeGold(-GetPrice());
				if (chestType != EChest.Free && chestType != EChest.FreeCrypt) MoneyUtility.IncreaseChestPrice();
			}
			Destroy(gameObject);
		}

		public override string GetInteractString()
		{
			return string.Format(CanAfford() ? "{0}<sprite name=gold></color>" : "<color=red>{0}<sprite name=gold></color>", GetPrice());
		}

		private void FixedUpdate()
		{
			if (detectInteractables == null) return;
			if (!CanAfford()) isHoveringAndCantAfford = true;
			else if (isHoveringAndCantAfford) { isHoveringAndCantAfford = false; RefreshInteractable(); }
		}

		public override Color GetColor()
		{
			return CanAfford() ? new Color32(255, 215, 69, 255) : new Color32(189, 34, 34, 255);
		}

		private int GetPrice()
		{
			return chestType == EChest.Free || chestType == EChest.FreeCrypt ? 0 : MoneyUtility.GetChestPrice();
		}

		private bool CanAfford()
		{
			return MyPlayer.Instance.inventory.gold >= GetPrice();
		}

		public override bool ShowInDebug()
		{
			return isShownInDebug;
		}

		public override string GetDebugName()
		{
			return isInCrypt ? debugNameCrypt : debugName;
		}

		private void OnDisable()
		{
			if (ShowInDebug() && !opening) A_DebugDisable?.Invoke(GetDebugName());
		}

		public override bool CanInteract()
		{
			return !ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS) && base.CanInteract();
		}
	}
}
