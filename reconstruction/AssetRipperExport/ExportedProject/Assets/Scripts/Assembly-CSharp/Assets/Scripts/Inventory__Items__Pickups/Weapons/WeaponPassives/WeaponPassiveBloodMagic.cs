using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Utility;
using Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponPassives
{
	public class WeaponPassiveBloodMagic : WeaponPassive
	{
		private int stacks;

		private float stackChance = .05f;

		private static string bloodMagicDamageSource = EWeapon.BloodMagic.ToString();

		private static float maxRollsUpgradesPerMinute = 100f;

		private float rollCooldown;

		private float nextReadyTime;

		public override void Init()
		{
			Enemy.A_EnemyDied += OnEnemyDied;
		}

		public override void Cleanup()
		{
			Enemy.A_EnemyDied -= OnEnemyDied;
		}

		private void OnEnemyDied(Enemy enemy, DamageContainer dc)
		{
			if (dc == null || nextReadyTime > MyTime.time || dc.damageSource != bloodMagicDamageSource || MyRandom.random.NextDouble() >= stackChance) return;
			nextReadyTime = MyTime.time + rollCooldown;
			stacks++;
			MyPlayer.Instance.inventory.statInventory.ChangeMovingStat(bloodMagicDamageSource,
				new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Flat, modification = stacks });
			MyStats.AddValue(EMyStat.bloodMagicProcs, 1f);
		}

		public WeaponPassiveBloodMagic(WeaponBase weaponBase)
			: base(weaponBase)
		{
			rollCooldown = maxRollsUpgradesPerMinute / 60f;
		}
	}
}
