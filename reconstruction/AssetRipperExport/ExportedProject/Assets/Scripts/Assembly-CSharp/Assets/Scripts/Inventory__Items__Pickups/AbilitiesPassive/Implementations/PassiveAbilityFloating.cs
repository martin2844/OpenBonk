using UnityEngine.Localization;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityFloating : PassiveAbility
	{
		private float floatingDamagePerLevel = 0.01f;

		private float lastSetDamage;

		private float updateDamageAtTime;

		private float updateDamageCooldown = 0.5f;

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
			if (MyPlayer.Instance == null) return;
			var player = MyPlayer.Instance;
			var movement = player.playerMovement;
			if (player.playerInput.IsHoldingJump() && movement.CanFloat())
			{
				var body = movement.GetRb();
				var velocity = body.velocity;
				velocity.y = -2f;
				body.velocity = velocity;
			}
			float damage = movement.IsTouchingGround() ? 0f : player.inventory.GetCharacterLevel() * floatingDamagePerLevel;
			if (MyTime.time < updateDamageAtTime) return;
			updateDamageAtTime = MyTime.time + updateDamageCooldown;
			if (Mathf.Abs(damage - lastSetDamage) <= 0.005f) return;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = damage });
			lastSetDamage = damage;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Float;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? "",
				["value1"] = string.Format("{0}%", floatingDamagePerLevel * 100f)
			});
		}
	}
}
