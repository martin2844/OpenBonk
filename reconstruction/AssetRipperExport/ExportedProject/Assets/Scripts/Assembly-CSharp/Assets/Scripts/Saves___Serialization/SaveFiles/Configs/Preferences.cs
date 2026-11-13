using System.Collections.Generic;
using System;
using Assets.Scripts._Data.Hats;

namespace Assets.Scripts.Saves___Serialization.SaveFiles.Configs
{
	public class Preferences
	{
		public Dictionary<ECharacter, int> characterSkins = new Dictionary<ECharacter, int>();

		public Dictionary<ECharacter, EHat> characterHats = new Dictionary<ECharacter, EHat>();

		public ECharacter selectedCharacter;

		public bool hasShownUnlocks;

		public bool hasShownQuests;

		public bool hasShownShop;

		public bool hasShownLeaderboards;

		public bool hasShownQuickQuests;

		public bool hasShownWarningForChestSkip;

		public void Init()
		{
			foreach (ECharacter character in Enum.GetValues(typeof(ECharacter)))
				if (!characterSkins.ContainsKey(character)) characterSkins.Add(character, 0);
			foreach (ECharacter character in Enum.GetValues(typeof(ECharacter)))
				if (!characterHats.ContainsKey(character)) characterHats.Add(character, EHat.None);
		}

		public void SetCharacterHat(ECharacter character, EHat hat)
		{
			characterHats[character] = hat;
		}

		public int GetCharacterSkin(ECharacter character)
		{
			characterSkins ??= new Dictionary<ECharacter, int>();
			if (!characterSkins.TryGetValue(character, out int skin)) characterSkins[character] = skin = 0;
			return skin;
		}

		public EHat GetCharacterHat(ECharacter character)
		{
			if (!characterHats.ContainsKey(character)) characterHats.Add(character, EHat.None);
			return characterHats[character];
		}
	}
}
