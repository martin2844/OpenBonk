using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts._Data;
using Assets.Scripts._Data.Progression;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Tools
{
	public class AchievementGenerator : MonoBehaviour
	{
		public LocalizedString stageBossLocalizedKey;

		public LocalizedString tierLocalizedKey;

		public LocalizedString finalBossLocalizedKey;

		public LocalizedString rankLocalizedKey;

		public LocalizedString localizedKills;

		public LocalizedString localizedLevel;

		public LocalizedString localizedFinalBoss;

		public LocalizedString localizedSpeedrun;

		public LocalizedString localizedGold;

		public DataManager dataManager;

		private static Dictionary<(ECharacter, ESkinType), string> skinAchievementNameCache = new Dictionary<(ECharacter, ESkinType), string>();

		private string GetPath()
		{
			return null;
		}

		public static int GetSkinAchValue(ESkinType skinType)
		{
			switch (skinType)
			{
				case ESkinType.Kills: return 15000;
				case ESkinType.Speedrun: return 6;
				case ESkinType.FinalBoss: return 0;
				case ESkinType.Gold: return 100;
				case ESkinType.Extra1: return 50000;
				case ESkinType.Extra2: return 100000;
				case ESkinType.Extra3: return 250000;
				case ESkinType.Extra4: return 500000;
				default: return 0;
			}
		}

		private string GetSkinAchValueStat(ESkinType skinType, ECharacter character)
		{
			return null;
		}

		private LocalizedString GetLocalizedDescription(ESkinType skinType)
		{
			return null;
		}

		private List<LocalizationKey> GetLocalizedKeys(ESkinType skinType, CharacterData characterData)
		{
			return null;
		}

		public static string GetSkinAchievementName(ECharacter character, ESkinType skinType)
		{
			var key = (character, skinType);
			if (!skinAchievementNameCache.TryGetValue(key, out string result))
			{
				string name = character.ToString();
				result = string.Format("a_skin_{0}{1}", char.ToLower(name[0]) + name.Substring(1), skinType);
				skinAchievementNameCache[key] = result;
			}
			return result;
		}

		private static EAchievementDifficulty GetSkinDifficulty(ESkinType skinType)
		{
			return default(EAchievementDifficulty);
		}
	}
}
