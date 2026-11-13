using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Saves___Serialization.Progression.Stats;

namespace Assets.Scripts.Inventory__Items__Pickups
{
	public class PlayerHealth
	{
		public static int maxMaxHp = 50000;

		public int hp;

		public int maxHp;

		public float overheal;

		public float maxOverheal;

		public float shield;

		public float maxShield;

		public static Action<PlayerHealth, DamageContainer, bool> A_TakeDamage;

		public static Action<PlayerHealth, float, bool> A_Heal;

		public static Action<PlayerHealth> A_MaxValuesChanged;

		public static Action<PlayerHealth> A_OverhealUpdate;

		public static Action A_CooldownOver;

		public static Action A_Died;

		public static Action<Enemy> A_Evaded;

		public static Action<DamageContainer, bool> A_CheckStopDamage;

		private float baseHp = 100f;

		private float baseShield = 25f;

		private string lvlHpMovingStatName = "LevelHp";

		private float minFallDamageSpeed = 30f;

		private float maxFallDamageSpeed = 140f;

		public float fallDamageTakenAtTime;

		public const string fallDamageSource = "fallDamage";

		private const string externalDamageSource = "Enemy";

		public static Action A_StoppedDamage;

		public static Action A_DamagePlayerCalled;

		public static HashSet<string> selfDamageSources = new HashSet<string> { "Kevin", healthRegenDamageSource };

		public static string thornsDamageSource = "Thorns";

		private float shieldRechargeAtTime;

		private float shieldRegenCooldownTime = 6f;

		public const float damageCooldownTime = 0.15f;

		public static Action<Enemy, int> A_LifestealProc;

		private int lifestealHeal;

		public static Action<int> A_LifestealHealing;

		private float leftOverHeal;

		public const string healthRegenDamageSource = "HealthRegen";

		private float nextCheckDeadTime;

		private float checkDeadInterval = 1f;

		private float overhealRemovalFractionPerSecond = 0.03f;

		private float shieldHealingPerTick = 10f;

		private float shieldHealingValue;

		private float healingValue;

		private float healingTime;

		private float healInterval;

		private float healPerInterval;

		private float healingPerMinute = 10f;

		private int maxIntervalsPerMinute;

		private float damageCooldown;

		public PlayerHealth(PlayerStatsNew playerStats)
		{
			maxIntervalsPerMinute = Mathf.RoundToInt(1f / Time.fixedDeltaTime) * 60;
			maxHp = (int)playerStats.GetStat(EStat.MaxHealth);
			maxShield = (int)playerStats.GetStat(EStat.Shield);
			hp = maxHp;
			shield = 0f;
			shieldRechargeAtTime = MyTime.time + shieldRegenCooldownTime;
			UpdateRegenValues(playerStats.GetStat(EStat.HealthRegen));
			PlayerStatsNew.A_StatUpdate += OnStatUpdate;
			PlayerMovement.A_Landed += OnPlayerLanded;
			Pickup.A_PickupTriggered += OnPickup;
			Enemy.A_Damage += OnEnemyDamaged;
			PlayerXp.A_LevelUp += OnLevelUp;
		}

		public void OnDestroy()
		{
			PlayerStatsNew.A_StatUpdate -= OnStatUpdate;
			PlayerMovement.A_Landed -= OnPlayerLanded;
			Pickup.A_PickupTriggered -= OnPickup;
			Enemy.A_Damage -= OnEnemyDamaged;
			PlayerXp.A_LevelUp -= OnLevelUp;
		}

		private void OnPickup(Pickup pickup)
		{
			if (pickup.ePickup == Assets.Scripts.Inventory__Items__Pickups.Pickups.EPickup.Health)
				Heal((int)(maxHp * 0.33f * PlayerStats.GetStat(EStat.PowerupBoostMultiplier)), false);
		}

		private void OnStatUpdate(EStat stat)
		{
			if (GameManager.Instance == null) return;
			if (stat == EStat.HealthRegen) UpdateRegenValues();
			else if (stat == EStat.MaxHealth || stat == EStat.Shield || stat == EStat.Overheal) UpdateMaxValues();
		}

		private void OnLevelUp(int lvl)
		{
			MyPlayer.Instance.inventory.statInventory.ChangeMovingStat(lvlHpMovingStatName, new StatModifier
			{
				stat = EStat.MaxHealth,
				modifyType = EStatModifyType.Flat,
				modification = lvl
			});
			UpdateMaxValues();
		}

		private void UpdateRegenValues(float forceValue = 0f)
		{
			float value = forceValue == 0f ? PlayerStats.GetStat(EStat.HealthRegen) : forceValue;
			healingPerMinute = value;
			shieldHealingPerTick = Mathf.Max(value * 2.5f, 25f);
			UpdateHealthRegen();
		}

		private void OnPlayerLanded(float speed)
		{
			MyPlayer player = MyPlayer.Instance;
			if (player.character == ECharacter.Calcium || player.character == ECharacter.TonyMcZoom || player.playerMovement.IsCrouching())
			{
				if (player.playerMovement.onRamp) return;
			}
			else if (player.character == ECharacter.Spaceman) return;
			float jumpHeight = PlayerStats.GetStat(EStat.JumpHeight);
			float minimum = minFallDamageSpeed + jumpHeight;
			if (speed < minimum) return;
			int damage = (int)((speed - minimum) / (maxFallDamageSpeed + jumpHeight) * 80f) + 10;
			damage -= (int)(damage * PlayerStats.GetStat(EStat.FallDamageReduction));
			damage = Mathf.Min(damage, hp - 1);
			if (damage > 0) DamagePlayerExternal(damage, 0f, Vector3.up, false, fallDamageSource);
			EffectManager.Instance.PlayerLandHard();
			fallDamageTakenAtTime = MyTime.time;
		}

		private void UpdateMaxValues()
		{
			if (GameManager.Instance == null) return;
			int oldMaxHp = maxHp;
			maxHp = Mathf.Min((int)PlayerStats.GetStat(EStat.MaxHealth), maxMaxHp);
			maxShield = Mathf.Min((int)PlayerStats.GetStat(EStat.Shield), maxMaxHp);
			hp = Mathf.Min(hp, maxHp);
			shield = Mathf.Min(shield, maxShield);
			Heal(maxHp - oldMaxHp);
			maxOverheal = (int)(maxHp * PlayerStats.GetStat(EStat.Overheal));
			A_MaxValuesChanged?.Invoke(this);
		}

		public void DamagePlayer(Enemy enemy, Vector3 direction, DcFlags flags = DcFlags.None)
		{
			if (CanTakeDamage()) Damage(DamageUtility.GetPlayerDamage(enemy, direction, flags), false);
		}

		public void DamagePlayerExternal(float damage, float knockback, Vector3 direction, bool ignoreShield = false, string damageSource = "Enemy", DcFlags flags = DcFlags.None, EDamageEffect damageEffect = EDamageEffect.None, Enemy enemy = null)
		{
			if (!CanTakeDamage() || MyPlayer.Instance.inventory?.statusEffects?.HasStatusEffect(EStatusEffect.Invulnerability) == true) return;
			DamageContainer dc = DamageUtility.GetPlayerDamage(damage, knockback, direction, null, damageSource, flags);
			dc.enemy = enemy;
			dc.damageEffect = damageSource == "Poison" ? EDamageEffect.Poison : EDamageEffect.None;
			Damage(dc, ignoreShield);
		}

		private void Damage(DamageContainer dc, bool ignoreShield)
		{
			A_DamagePlayerCalled?.Invoke();
			if (!CanTakeDamage()) { A_StoppedDamage?.Invoke(); return; }
			if (CheckStopDamage(dc, ignoreShield)) { A_StoppedDamage?.Invoke(); return; }
			if (MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.inventory.playerHealth == this)
				MyPlayer.Instance.inventory.itemInventory?.BeforePlayerDamage(dc);
			Retaliate(dc);
			if (dc.damage <= 0f) return;
			bool shieldDamage = shield > 0f && !ignoreShield;
			if (shieldDamage) shield = (int)Mathf.Max(0f, shield - dc.damage);
			else
			{
				float remaining = dc.damage;
				if (overheal > 0f)
				{
					float previous = overheal;
					overheal = Mathf.Clamp(overheal - remaining, 0f, maxOverheal);
					remaining -= previous;
				}
				if (remaining > 0f) hp = (int)Mathf.Max(0f, hp - remaining);
				if (hp <= 0)
				{
					var inventory = MyPlayer.Instance.inventory;
					if (inventory.itemInventory.items.ContainsKey(EItem.ZaWarudo))
					{
						float duration = ItemZaWarudo.freezeTime;
						inventory.statusEffects.AddNewEffect(new StatusEffect(EStatusEffect.TimeFreeze, MyTime.time + duration, Array.Empty<StatModifier>()), duration);
						inventory.itemInventory.RemoveItem(EItem.ZaWarudo);
						hp = maxHp;
					}
					else PlayerDied();
				}
			}
			CheckDamageCooldown(dc);
			CheckShieldRecharge(dc);
			A_TakeDamage?.Invoke(this, dc, shieldDamage);
		}

		private void CheckDamageCooldown(DamageContainer dc)
		{
			if (dc.damageSource == externalDamageSource) damageCooldown = GetDamageCooldown();
		}

		private void CheckShieldRecharge(DamageContainer dc)
		{
			if (selfDamageSources == null || !selfDamageSources.Contains(dc.damageSource)) shieldRechargeAtTime = MyTime.time + shieldRegenCooldownTime;
		}

		public void KillPlayer()
		{
			if (hp <= 0) return;
			hp = 0;
			shield = 0f;
			overheal = 0f;
			A_Died?.Invoke();
		}

		public bool CheckStopDamage(DamageContainer dc, bool ignoreShield)
		{
			var player = MyPlayer.Instance;
			if ((dc.flags & DcFlags.BypassAegis) == 0 && player.playerConstantAttacks != null && player.playerConstantAttacks.aegisAttack != null && player.playerConstantAttacks.aegisAttack.IsActive())
			{
				if ((dc.flags & DcFlags.BossDamage) != 0)
				{
					UseAegis(dc, MyColorUtility.weakAegisColor, "Weak Block");
					dc.damage *= (dc.flags & DcFlags.FinalBossDamage) != 0 ? .75f : .33f;
				}
				else
				{
					UseAegis(dc, MyColorUtility.aegisColor, "Block");
					if (player.character == ECharacter.Athena) return true;
					dc.damage *= .05f;
					if (dc.damage > 0f) dc.damage = Mathf.Max(dc.damage, 1f);
					dc.knockback = 0f;
				}
			}
			if ((dc.flags & DcFlags.BypassEvade) == 0 && DamageUtility.CheckEvade(dc.enemy))
			{
				Evade(dc);
				return true;
			}
			if (MyPlayer.Instance.inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze)) return true;
			A_CheckStopDamage?.Invoke(dc, ignoreShield);
			return dc.damage <= 0f;
		}

		private void UseAegis(DamageContainer dc, Color color, string text = "Block")
		{
			var player = MyPlayer.Instance;
			EffectManager.Instance.PopupText(text, color, player.transform.position, 22);
			Vector3 source = dc.enemy != null ? dc.enemy.GetFeetPosition() : player.transform.position - dc.direction;
			player.playerConstantAttacks.aegisAttack.UseShield(source);
			ControllerShaker.Shake(0, .3f, .1f);
			MyStats.AddValue(EMyStat.aegisBlocks, 1f);
		}

		public bool WillDamageKill(DamageContainer dc, bool ignoreShield)
		{
			return WillDamageKill(dc.damage, ignoreShield);
		}

		public bool WillDamageKill(float damage, bool ignoreShield)
		{
			if (!ignoreShield && shield > 0f) return false;
			return hp + overheal - damage <= 0f;
		}

		private void Evade(DamageContainer dc)
		{
			Retaliate(dc);
			var player = MyPlayer.Instance;
			bool phantom = player.inventory.itemInventory.items.ContainsKey(Assets.Scripts.Inventory__Items__Pickups.Items.EItem.PhantomShroud);
			if (player.playerSfxs != null) player.playerSfxs.Evade(phantom);
			A_Evaded?.Invoke(dc.enemy);
		}

		public void Retaliate(DamageContainer dc)
		{
			if (dc.enemy == null) return;
			int thorns = (int)PlayerStats.GetStat(EStat.Thorns);
			if (thorns <= 0) return;
			Enemy enemy = dc.enemy;
			DamageContainer retaliation = WeaponUtility.GetDamageContainer(null, thorns, 1f, thornsDamageSource, -dc.direction, enemy);
			enemy.DamageFromPlayerOther(retaliation);
			EffectManager.Instance.EnemyHitEffect(enemy.GetCenterPosition(), Vector3.zero, true, thornsDamageSource, null, true);
		}

		private float GetDamageCooldown()
		{
			return damageCooldownTime * PlayerStats.GetStat(EStat.DamageCooldownMultiplier);
		}

		private void OnEnemyDamaged(Enemy enemy, DamageContainer dc)
		{
			if (CanLifesteal() && dc.damageEffect != EDamageEffect.Bloodmark) TryLifestealHit(enemy, dc);
		}

		private void TryLifestealHit(Enemy enemy, DamageContainer dc)
		{
			float value = PlayerStats.GetStat(EStat.Lifesteal);
			if (value <= 0f) return;
			int amount = Mathf.FloorToInt(value);
			if (global::Utility.MyRandom.random.NextDouble() < value - amount) amount++;
			lifestealHeal += amount;
			A_LifestealProc?.Invoke(enemy, amount);
		}

		private void ApplyLifesteal()
		{
			int healed = Heal(lifestealHeal);
			lifestealHeal = 0;
			if (healed > 0) A_LifestealHealing?.Invoke(healed);
		}

		public int Heal(float amount, bool allowOverheal = true)
		{
			var player = MyPlayer.Instance;
			if (player != null && player.inventory != null && player.inventory.statusEffects != null && player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Bleed)) return 0;
			float adjusted = amount * PlayerStats.GetStat(EStat.HealingMultiplier) + leftOverHeal;
			int whole = Mathf.FloorToInt(adjusted);
			leftOverHeal = adjusted - whole;
			if (hp >= maxHp && allowOverheal && overheal < maxOverheal)
			{
				int added = Mathf.FloorToInt(Mathf.Min(whole, maxOverheal - overheal));
				if (added <= 0) return 0;
				overheal = Mathf.Clamp(overheal + whole, 0f, maxOverheal);
				A_Heal?.Invoke(this, added, false);
				return added;
			}
			if (whole <= 0 || hp <= 0 || hp >= maxHp) return 0;
			int oldHp = hp;
			hp = Mathf.Clamp(hp + whole, 0, maxHp);
			A_Heal?.Invoke(this, whole, false);
			return hp - oldHp;
		}

		public void Tick()
		{
			if (damageCooldown > 0f)
			{
				damageCooldown -= MyTime.fixedDeltaTime;
				if (damageCooldown <= 0f) A_CooldownOver?.Invoke();
			}
			ApplyLifesteal();
			healingTime += MyTime.fixedDeltaTime;
			if (healingTime >= healInterval)
			{
				healingTime -= healInterval;
				healingValue += healPerInterval;
				if (healingValue >= 1f)
				{
					int whole = Mathf.FloorToInt(healingValue);
					healingValue -= whole;
					Heal(whole, false);
				}
				else if (healingValue <= -1f)
				{
					int whole = Mathf.FloorToInt(healingValue);
					healingValue -= whole;
					int damage = Mathf.Abs(Mathf.Max(whole, 1 - hp));
					if (damage > 0 && hp - damage > 0)
						DamagePlayerExternal(damage, 0f, Vector3.zero, true, healthRegenDamageSource, DcFlags.BypassAll);
				}
			}
			if (MyTime.time >= shieldRechargeAtTime && shield < maxShield)
			{
				shield = Mathf.Clamp(shield + shieldHealingPerTick * MyTime.fixedDeltaTime, 0f, maxShield);
				A_Heal?.Invoke(this, shieldHealingPerTick, true);
			}
			if (overheal > 0f)
			{
				overheal = Mathf.Clamp(overheal - overhealRemovalFractionPerSecond * maxOverheal * MyTime.fixedDeltaTime, 0f, maxOverheal);
				A_OverhealUpdate?.Invoke(this);
			}
			CheckDead();
		}

		private void CheckDead()
		{
			if (nextCheckDeadTime > MyTime.time) return;
			nextCheckDeadTime = MyTime.time + checkDeadInterval;
			if (hp <= 0 && GameManager.Instance != null && !GameManager.Instance.isGameOver) PlayerDied();
		}

		private void UpdateHealthRegen()
		{
			int intervals = (int)Mathf.Min(Mathf.Abs(healingPerMinute), maxIntervalsPerMinute);
			healInterval = Mathf.Abs(60f / intervals);
			healPerInterval = Mathf.Abs(healingPerMinute) / intervals * (healingPerMinute >= 0f ? 1f : -1f);
			healingTime = 0f;
		}

		public void PlayerDied()
		{
			hp = 0;
			shield = 0f;
			overheal = 0f;
			A_Died?.Invoke();
		}

		public bool IsDead()
		{
			return hp <= 0;
		}

		public bool CanTakeDamage()
		{
			return hp > 0 && !DamageCooldown() && MyPlayer.Instance.CanTakeDamage();
		}

		public int GetCombinedHp()
		{
			return (int)(hp + overheal);
		}

		public int GetCombinedMaxHp()
		{
			return (int)(maxHp + overheal);
		}

		public float GetHpRatio()
		{
			return (float)hp / maxHp;
		}

		public bool DamageCooldown()
		{
			return damageCooldown > 0.1f;
		}

		public bool CanHeal()
		{
			return hp > 0 && hp < maxHp;
		}

		public bool CanLifesteal()
		{
			return hp < maxHp || overheal < maxOverheal;
		}
	}
}
