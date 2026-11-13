using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Stats
{
	public class PlayerStatusEffects
	{
		public Dictionary<EStatusEffect, StatusEffect> statusEffects = new Dictionary<EStatusEffect, StatusEffect>();
		public static Action<EStat> A_StatusModifiedStat;
		public static Action<EStatusEffect, bool> A_StatusEffectAdded;
		public static Action<EStatusEffect> A_StatusEffectRemoved;
		public const string poisonEffectName = "Poison";
		public const string bleedEffectName = "Bleed";
		private float nextBleedTime;
		private float bleedInterval = 0.5f;
		private float nextPoisonTime;
		private float poisonInterval = 1f;

		public PlayerStatusEffects()
		{
			Pickup.A_PickupTriggered += OnPickupTriggered;
			LevelupScreen.A_LevelUpClose += OnLevelupScreenDone;
		}

		public void OnDestroy()
		{
			Pickup.A_PickupTriggered -= OnPickupTriggered;
			LevelupScreen.A_LevelUpClose -= OnLevelupScreenDone;
		}

		public void Tick()
		{
			if (statusEffects.Count == 0) return;
			foreach (StatusEffect effect in statusEffects.Values.Where(e => MyTime.time >= e.expirationTime).ToList())
				RemoveStatusEffect(effect.eStatusEffect);
			TickEffects();
		}

		public void AddNewEffect(StatusEffect statusEffect, float statusLengthTime)
		{
			bool isNew = !statusEffects.TryGetValue(statusEffect.eStatusEffect, out StatusEffect existing);
			if (isNew) statusEffects.Add(statusEffect.eStatusEffect, statusEffect);
			else existing.expirationTime = MyTime.time + statusLengthTime;
			foreach (StatModifier modifier in statusEffect.modifiers) A_StatusModifiedStat?.Invoke(modifier.stat);
			A_StatusEffectAdded?.Invoke(statusEffect.eStatusEffect, isNew);
		}

		private void RemoveStatusEffect(EStatusEffect eStatusEffect)
		{
			statusEffects.TryGetValue(eStatusEffect, out StatusEffect effect);
			statusEffects.Remove(eStatusEffect);
			if (effect != null)
				foreach (StatModifier modifier in effect.modifiers) A_StatusModifiedStat?.Invoke(modifier.stat);
			A_StatusEffectRemoved?.Invoke(eStatusEffect);
		}

		public void RemoveAllStatusEffects()
		{
			foreach (EStatusEffect effect in statusEffects.Keys.ToList()) RemoveStatusEffect(effect);
		}

		public bool HasStatusEffect(EStatusEffect effect) => statusEffects.ContainsKey(effect);
		public void TestPickup(EPickup ePickup) => OnPickupTriggered(ePickup);
		private void OnPickupTriggered(Pickup pickup) => OnPickupTriggered(pickup.ePickup);

		private void AddTimed(EStatusEffect type, float duration, params StatModifier[] modifiers)
		{
			AddNewEffect(new StatusEffect(type, MyTime.time + duration, modifiers), duration);
		}

		private static StatModifier Modifier(EStat stat, EStatModifyType type, float value) =>
			new StatModifier { stat = stat, modifyType = type, modification = value };

		public void SlowPlayer(float time) => AddTimed(EStatusEffect.Slow, time, Modifier(EStat.MoveSpeedMultiplier, EStatModifyType.Multiplication, 0.5f));
		public void FreezePlayer(float time) => AddTimed(EStatusEffect.Freeze, time, Modifier(EStat.MoveSpeedMultiplier, EStatModifyType.Multiplication, 0.075f));
		public void BleedPlayer(float duration) => AddTimed(EStatusEffect.Bleed, duration);
		public void PoisonPlayer(float duration)
		{
			var player = MyPlayer.Instance;
			if (player != null && player.character == ECharacter.Athena && player.playerConstantAttacks != null &&
				player.playerConstantAttacks.aegisAttack != null && player.playerConstantAttacks.aegisAttack.IsActive()) return;
			AddTimed(EStatusEffect.Poison, duration);
		}
		public void BossPoisonPlayer(float duration) => AddTimed(EStatusEffect.BossPoison, duration);

		private void OnPickupTriggered(EPickup ePickup)
		{
			float boost = PlayerStats.GetStat(EStat.PowerupBoostMultiplier);
			switch (ePickup)
			{
				case EPickup.Time: AddTimed(EStatusEffect.TimeFreeze, 12f * boost); break;
				case EPickup.Shield: AddTimed(EStatusEffect.Shield, 15f * boost, Modifier(EStat.DamageReductionMultiplier, EStatModifyType.Flat, 1f)); break;
				case EPickup.Rage: AddTimed(EStatusEffect.Rage, 15f * boost,
					Modifier(EStat.DamageMultiplier, EStatModifyType.Multiplication, 1.5f * boost),
					Modifier(EStat.AttackSpeed, EStatModifyType.Multiplication, 2f * boost)); break;
				case EPickup.Haste: AddTimed(EStatusEffect.Haste, 20f * boost, Modifier(EStat.MoveSpeedMultiplier, EStatModifyType.Multiplication, 1.75f * boost)); break;
				case EPickup.Stonks: AddTimed(EStatusEffect.Stonks, 15f * boost); break;
			}
		}

		private void OnLevelupScreenDone() => AddTimed(EStatusEffect.Invulnerability, 0.25f, Modifier(EStat.DamageReductionMultiplier, EStatModifyType.Flat, 1f));

		private void TickEffects()
		{
			var health = MyPlayer.Instance.inventory.playerHealth;
			if (HasStatusEffect(EStatusEffect.Bleed) && MyTime.time > nextBleedTime)
			{
				nextBleedTime = MyTime.time + bleedInterval;
				if (!health.WillDamageKill(5f, true)) health.DamagePlayerExternal(5f, 0f, Vector3.zero, true, bleedEffectName, DcFlags.BypassAll);
			}
			if (MyTime.time < nextPoisonTime) return;
			if (HasStatusEffect(EStatusEffect.Poison)) ApplyPoison(health, 0.01f);
			if (HasStatusEffect(EStatusEffect.BossPoison)) ApplyPoison(health, 0.03f);
		}

		private void ApplyPoison(Assets.Scripts.Inventory__Items__Pickups.PlayerHealth health, float fraction)
		{
			nextPoisonTime = MyTime.time + poisonInterval;
			int damage = Mathf.RoundToInt(Mathf.Max(1f, health.maxHp * fraction));
			if (!health.WillDamageKill(damage, true)) health.DamagePlayerExternal(damage, 0f, Vector3.zero, true, poisonEffectName, DcFlags.BypassAll | DcFlags.IgnoreArmor, EDamageEffect.Poison);
		}
	}
}
