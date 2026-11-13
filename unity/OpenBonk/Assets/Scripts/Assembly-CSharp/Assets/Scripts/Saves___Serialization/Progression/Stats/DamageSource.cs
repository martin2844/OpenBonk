using UnityEngine;
using System;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	public class DamageSource
	{
		public string damageSource;

		public float addedAtTime;

		public float damage;

		public DamageSource(string damageSource, float addedAtTime)
		{
			this.damageSource = damageSource;
			this.addedAtTime = addedAtTime;
		}

		public void AddDamage(float d)
		{
			damage += d;
		}

		public int GetLevel()
		{
			var inventory = MyPlayer.Instance != null ? MyPlayer.Instance.inventory : null;
			if (DataManager.Instance != null && DataManager.Instance.TryGetExternalItemDamageSource(damageSource, out var external))
				return inventory?.itemInventory?.GetAmount(external.eItem) ?? 0;
			if (Enum.TryParse(damageSource, true, out EWeapon weapon)) return inventory?.weaponInventory?.GetWeaponLevel(weapon) ?? 0;
			if (Enum.TryParse(damageSource, true, out EItem item)) return inventory?.itemInventory?.GetAmount(item) ?? 0;
			return 1;
		}

		public Texture GetIcon()
		{
			if (DataManager.Instance != null)
			{
				if (DataManager.Instance.TryGetExternalItemDamageSource(damageSource, out var external)) return external.GetIcon();
				try
				{
					if (Enum.TryParse(damageSource, true, out EWeapon weapon)) return DataManager.Instance.GetWeapon(weapon)?.GetIcon();
					if (Enum.TryParse(damageSource, true, out EItem item)) return DataManager.Instance.GetItem(item)?.GetIcon();
				}
				catch (System.Collections.Generic.KeyNotFoundException) { return null; }
			}
			var icons = IconManager.Instance;
			if (icons == null) return null;
			if (Enum.TryParse(damageSource, true, out EDebuff debuff)) return icons.GetDebuffIcon(debuff);
			if (damageSource == "Thorns") return icons.thornsIcon;
			if (damageSource == "Zap") return icons.zapIcon;
			if (damageSource == "Shadowstep") return icons.shadowStepIcon;
			return damageSource == "Bullseye" ? icons.bullseyeIcon : icons.questionMark;
		}
	}
}
