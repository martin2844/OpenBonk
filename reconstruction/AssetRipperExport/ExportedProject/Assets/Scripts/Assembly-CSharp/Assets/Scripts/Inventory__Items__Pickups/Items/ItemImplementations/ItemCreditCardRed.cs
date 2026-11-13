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
	public class ItemCreditCardRed : ItemBase
	{
		private float damagePerChestAmount = .025f;

		private float damagePerChest;

		private float accumulatedDamage;

		protected override void OnInitOrAmountChanged()
		{
			damagePerChest = damagePerChestAmount * amount;
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
			accumulatedDamage += damagePerChest;
			MyPlayer.Instance.inventory.statInventory.ChangeMovingStat(EItem.CreditCardRed.ToString(), new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = accumulatedDamage });
		}

		public ItemCreditCardRed(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "", ["value1"] = string.Format("+{0}%", damagePerChestAmount * 100f) });
		}
	}
}
