using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts._Data.Tomes;

namespace Assets.Scripts.Inventory__Items__Pickups
{
	public class TomeInventory
	{
		private bool isMaxed;

		public static Action<ETome, EStat> A_TomeUpgrade;

		public Dictionary<ETome, int> tomeLevels = new Dictionary<ETome, int>();

		public Dictionary<EStat, HashSet<ETome>> statToTomes = new Dictionary<EStat, HashSet<ETome>>();

		public Dictionary<ETome, StatModifier> tomeUpgrade = new Dictionary<ETome, StatModifier>();

		public void AddTome(TomeData tomeData, List<StatModifier> upgradeOffer, ERarity rarity)
		{
			ETome tome = tomeData.eTome;
			if (!tomeLevels.ContainsKey(tome)) tomeLevels.Add(tome, 0);
			if (tomeLevels[tome] < tomeData.GetMaxLevel()) tomeLevels[tome]++;
			else UnityEngine.Debug.LogError($"Tome {tome} is already max level");
			EStat stat = tomeData.statModifier.stat;
			if (!statToTomes.ContainsKey(stat)) statToTomes.Add(stat, new HashSet<ETome>());
			if (!tomeUpgrade.ContainsKey(tome))
				tomeUpgrade.Add(tome, new StatModifier { stat = stat, modifyType = tomeData.statModifier.modifyType, modification = tomeData.statModifier.modifyType == EStatModifyType.Multiplication ? 1f : 0f });
			foreach (StatModifier modifier in upgradeOffer) tomeUpgrade[tome].modification += modifier.modification;
			statToTomes[stat].Add(tome);
			CheckMaxed();
			TomeUtility.CheckSpecialTomes(tomeData, rarity);
			A_TomeUpgrade?.Invoke(tome, stat);
		}

		public void AddMaxedTome(TomeData tomeData)
		{
			ETome tome = tomeData.eTome;
			tomeLevels[tome] = tomeData.GetMaxLevel();
			EStat stat = tomeData.statModifier.stat;
			if (!statToTomes.ContainsKey(stat)) statToTomes.Add(stat, new HashSet<ETome>());
			statToTomes[stat].Add(tome);
			A_TomeUpgrade?.Invoke(tome, stat);
		}

		public int GetTomeLevel(ETome tome)
		{
			return tomeLevels.TryGetValue(tome, out int level) ? level : 0;
		}

		public int GetNumTomes()
		{
			return tomeLevels.Count;
		}

		private void CheckMaxed()
		{
			isMaxed = true;
			foreach (ETome tome in tomeLevels.Keys) if (!IsMaxLevel(tome)) { isMaxed = false; break; }
		}

		private bool IsMaxLevel(ETome eTome)
		{
			return tomeLevels.TryGetValue(eTome, out int level) && level >= DataManager.Instance.GetTome(eTome).GetMaxLevel();
		}

		public bool IsMaxed()
		{
			if (tomeLevels.Count < InventoryUtility.GetNumAvailableTomeSlots()) return false;
			foreach (ETome tome in tomeLevels.Keys) if (!IsMaxLevel(tome)) return false;
			return true;
		}

		public bool HasTome(ETome eTome)
		{
			return tomeLevels.ContainsKey(eTome);
		}
	}
}
