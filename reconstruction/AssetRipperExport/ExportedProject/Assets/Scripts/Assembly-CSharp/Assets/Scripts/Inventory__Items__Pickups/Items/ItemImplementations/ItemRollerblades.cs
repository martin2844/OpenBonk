using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemRollerblades : ItemBase
	{
		private float cap;

		private float maxAttackSpeedPerAmount = .4f;

		private float updateStatsInterval = .25f;

		private float nextUpdateTime;

		protected override void OnInitOrAmountChanged()
		{
			cap = amount * maxAttackSpeedPerAmount;
		}

		public ItemRollerblades(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
			if (MyTime.time < nextUpdateTime) return;
			nextUpdateTime = MyTime.time + updateStatsInterval;
			var player = MyPlayer.Instance;
			float bonus = amount * Mathf.Clamp(player.playerMovement.GetSpeedHorizontal() / player.baseMovementSpeed - 1f, 0f, cap);
			SetStat(new StatModifier { stat = EStat.AttackSpeed, modifyType = EStatModifyType.Flat, modification = bonus });
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["stat1"] = EnumUtility.EnumToReadable(EStat.AttackSpeed) ?? "", ["value1"] = string.Format("{0}%", maxAttackSpeedPerAmount * 100f) };
		}
	}
}
