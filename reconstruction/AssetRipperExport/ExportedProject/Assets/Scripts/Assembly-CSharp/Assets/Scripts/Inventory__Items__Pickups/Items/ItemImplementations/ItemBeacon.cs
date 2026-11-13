using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using Assets.Scripts.Actors.Player;
using System.Collections.Generic;
using Assets.Scripts.UI.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBeacon : ItemBase
	{
		private int extraShrinesPerAmount = 2;

		private float healingRadiusPerAmount = 2f;

		private float healingFractionPerInterval = .025f;

		public float GetHealingPerInterval()
		{
			return healingFractionPerInterval * amount * MyPlayer.Instance.inventory.playerHealth.maxHp;
		}

		public float GetRadius()
		{
			return 8f + healingRadiusPerAmount * amount;
		}

		public int GetExtraShrines()
		{
			return amount * extraShrinesPerAmount;
		}

		public ItemBeacon(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		protected override void OnInitOrAmountChanged()
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
			var values = new Dictionary<string, string>
			{
				["charge_shrine"] = LocalizationUtility.GetLocalizedString("Game_Interactables", "CHARGE_SHRINE_NAME"),
				["value"] = string.Format("+{0}", extraShrinesPerAmount)
			};
			return localizedString.GetLocalizedString(values);
		}
	}
}
