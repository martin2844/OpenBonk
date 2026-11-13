using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemCreditCardGreen : ItemBase
	{
		private float luckPerChestPerAmount = .02f;

		private float luckPerChest;

		private float chestPriceIncreasePerAmount = .1f;

		private float accumulatedLuck;

		protected override void OnInitOrAmountChanged()
		{
			luckPerChest = luckPerChestPerAmount * amount;
			SetStat(new StatModifier { stat = EStat.ChestPriceMultiplier, modifyType = EStatModifyType.Flat, modification = chestPriceIncreasePerAmount * amount });
		}

		public override void Init()
		{
			InteractableChest.A_ChestOpened += OnChestWindowOpen;
			OpenChest.A_Open += OnChestWindowOpen;
		}

		public override void Cleanup()
		{
			InteractableChest.A_ChestOpened -= OnChestWindowOpen;
			OpenChest.A_Open -= OnChestWindowOpen;
		}

		private void OnChestWindowOpen()
		{
			accumulatedLuck += luckPerChest;
			MyPlayer.Instance.inventory.statInventory.ChangeMovingStat(EItem.CreditCardGreen.ToString(), new StatModifier { stat = EStat.Luck, modifyType = EStatModifyType.Flat, modification = accumulatedLuck });
		}

		public ItemCreditCardGreen(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Tick()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.Luck) ?? "", ["value1"] = string.Format("+{0}%", luckPerChestPerAmount * 100f), ["stat2"] = EnumUtility.EnumToReadable(EStat.ChestPriceMultiplier) ?? "", ["value2"] = string.Format("+{0}%", chestPriceIncreasePerAmount * 100f) });
		}
	}
}
