using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilitySpeedDemon : PassiveAbility
	{
		private float increaseInterval = 1f;

		private float increasePerInterval = .0085f;

		private float speedIncrease;

		private float nextInterval;

		private float cap = 1.5f;

		private float updateStatsInterval = .25f;

		private float nextUpdateDamageTime;

		private float damagePerSpeedMultiplier = .5f;

		private float damagePerLevel = .0075f;

		private int hitsToFullyResetSpeedMin = 4;

		private int hitsToFullyResetSpeedMax = 15;

		private int hitsToFullyResetSpeed = 4;

		private int levelPerHitIncrease = 10;

		public override void Init()
		{
			nextInterval = MyTime.time + increaseInterval;
			PlayerHealth.A_TakeDamage += OnDamage;
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_TakeDamage -= OnDamage;
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		public override void Tick()
		{
			if (!CanTick()) return;
			TickSpeedIncrease();
			TickDamageIncrease();
		}

		private void TickSpeedIncrease()
		{
			if (nextInterval > MyTime.time) return;
			nextInterval = MyTime.time + increaseInterval;
			float old = speedIncrease;
			speedIncrease = Mathf.Clamp(speedIncrease + increasePerInterval, 0f, cap);
			if (speedIncrease != old)
				SetStat(new StatModifier { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Multiplication, modification = 1f + speedIncrease });
		}

		private void TickDamageIncrease()
		{
			if (nextUpdateDamageTime > MyTime.time) return;
			nextUpdateDamageTime = MyTime.time + updateStatsInterval;
			var player = MyPlayer.Instance;
			float speedRatio = Mathf.Clamp01(player.playerMovement.GetSpeedHorizontal() * .75f / player.playerMovement.movementValues.GetMaxSpeed());
			float bonus = ((PlayerStats.GetStat(EStat.MoveSpeedMultiplier) - 1f) * damagePerSpeedMultiplier + player.inventory.GetCharacterLevel() * damagePerLevel) * speedRatio;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = bonus });
		}

		private bool CanTick()
		{
			return GameManager.Instance != null && !GameManager.Instance.isCrypt;
		}

		private void OnLevelup(int level)
		{
			hitsToFullyResetSpeed = Mathf.Clamp(hitsToFullyResetSpeedMin + Mathf.FloorToInt((float)level / levelPerHitIncrease), hitsToFullyResetSpeedMin, hitsToFullyResetSpeedMax);
		}

		private void OnDamage(PlayerHealth ph, DamageContainer dc, bool brokeShield)
		{
			if (dc.damageSource == PlayerHealth.fallDamageSource || dc.damageSource == DebuffPoison.poisonDamageSource || dc.damageSource == PlayerHealth.healthRegenDamageSource) return;
			nextInterval = MyTime.time + increaseInterval;
			speedIncrease = Mathf.Max(0f, speedIncrease - cap / hitsToFullyResetSpeed);
			SetStat(new StatModifier { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Multiplication, modification = 1f + speedIncrease });
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.SpeedDemon;
		}
	}
}
