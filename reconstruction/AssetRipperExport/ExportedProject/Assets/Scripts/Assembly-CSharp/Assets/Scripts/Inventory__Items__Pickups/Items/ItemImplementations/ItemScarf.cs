using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemScarf : ItemBase
	{
		private float damageAddPerAmount = .5f;

		private float damageAdd;

		private float lastValueSet;

		protected override void OnInitOrAmountChanged()
		{
			damageAdd = amount * damageAddPerAmount;
			if (MyPlayer.Instance != null) UpdateDamage(MyPlayer.Instance.playerMovement.grounded);
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		private void OnGroundedChange(bool grounded)
		{
			UpdateDamage(grounded);
		}

		private void UpdateDamage(bool grounded)
		{
			float value = grounded ? 0f : damageAdd;
			if (value == lastValueSet) return;
			lastValueSet = value;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Addition, modification = value });
		}

		public ItemScarf(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
			PlayerMovement.A_Grounded += OnGroundedChange;
		}

		public override void Cleanup()
		{
			PlayerMovement.A_Grounded -= OnGroundedChange;
		}

		public override void Tick()
		{
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "", ["value1"] = string.Format("{0}%", damageAddPerAmount * 100f) };
		}
	}
}
