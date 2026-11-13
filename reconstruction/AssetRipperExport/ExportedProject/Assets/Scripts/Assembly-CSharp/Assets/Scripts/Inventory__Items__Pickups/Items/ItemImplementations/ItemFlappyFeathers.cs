using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemFlappyFeathers : ItemBase
	{
		private float speedBoostPerAmount = 2.2f;

		private float jumpHeightAdditionPerAmount = .15f;

		private float speedBoost;

		private int extraJumpsPerAmount = 1;

		protected override void OnInitOrAmountChanged()
		{
			SetStat(new StatModifier { stat = EStat.JumpHeight, modifyType = EStatModifyType.Addition, modification = jumpHeightAdditionPerAmount * amount });
			SetStat(new StatModifier { stat = EStat.ExtraJumps, modifyType = EStatModifyType.Flat, modification = extraJumpsPerAmount * amount });
			speedBoost = speedBoostPerAmount * amount;
		}

		private void OnJumped(PlayerMovement pm)
		{
			if (pm == null || pm.GetRb() == null) return;
			Vector3 direction = pm.GetWishDir();
			direction.y = 0f;
			pm.GetRb().velocity += direction.normalized * speedBoost;
		}

		public override void Init()
		{
			PlayerMovement.A_Jumped += OnJumped;
		}

		public override void Cleanup()
		{
			PlayerMovement.A_Jumped -= OnJumped;
		}

		public ItemFlappyFeathers(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.ExtraJumps) ?? string.Empty });
		}
	}
}
