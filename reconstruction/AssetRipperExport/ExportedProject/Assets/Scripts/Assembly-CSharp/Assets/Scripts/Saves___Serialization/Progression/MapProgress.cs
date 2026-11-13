using System;
using System.Collections.Generic;

namespace Assets.Scripts.Saves___Serialization.Progression
{
	[Serializable]
	public class MapProgress
	{
		public HashSet<int> tierNotifications = new HashSet<int>();

		public HashSet<int> tierChallengeNotifications = new HashSet<int>();

		public bool newMapNotification;

		public int lastSelectTier;

		public List<int> completedTiers = new List<int>();

		public Dictionary<int, HashSet<ECharacter>> tierCompletionsWithCharacters = new Dictionary<int, HashSet<ECharacter>>();

		public Dictionary<int, int> numRunsByTier = new Dictionary<int, int>();

		public Dictionary<int, float> tierHighscores = new Dictionary<int, float>();

		public Dictionary<int, float> tierFastestTimes = new Dictionary<int, float>();

		public void OnRunFinished(ECharacter character, bool victory, int tier)
		{
			numRunsByTier.TryGetValue(tier, out int count);
			numRunsByTier[tier] = count + 1;
			if (!victory) return;
			if (!tierCompletionsWithCharacters.TryGetValue(tier, out HashSet<ECharacter> characters))
				tierCompletionsWithCharacters[tier] = characters = new HashSet<ECharacter>();
			characters.Add(character);
			CompleteTier(tier);
		}

		public int GetNumTierRuns(int tier)
		{
			if (!numRunsByTier.ContainsKey(tier)) numRunsByTier[tier] = 0;
			return numRunsByTier[tier];
		}

		public List<ECharacter> GetTierCompletionCharacters(int tier)
		{
			if (!tierCompletionsWithCharacters.TryGetValue(tier, out HashSet<ECharacter> characters))
				tierCompletionsWithCharacters[tier] = characters = new HashSet<ECharacter>();
			return new List<ECharacter>(characters);
		}

		public void CompleteTier(int tier)
		{
			if (completedTiers.Contains(tier)) return;
			completedTiers.Add(tier);
			tierNotifications.Add(tier + 1);
			tierChallengeNotifications.Add(tier);
		}

		public bool IsTierCompleted(int tier)
		{
			return completedTiers.Contains(tier);
		}

		public void SetCompletedTime(float time)
		{
			if (!tierFastestTimes.TryGetValue(lastSelectTier, out float previous) || time < previous)
				tierFastestTimes[lastSelectTier] = time;
		}

		public void SetKills(int kills)
		{
			if (!tierHighscores.TryGetValue(lastSelectTier, out float previous) || kills > previous)
				tierHighscores[lastSelectTier] = kills;
		}

		public string GetTierHighscoreString(int tier)
		{
			return tierHighscores.TryGetValue(tier, out float score) ? "<size=110%><sprite name=skull></size> " + DamageNumbers.FormatDamageNumber(score) : "-";
		}

		public string GetTierFastestTimeString(int tier)
		{
			if (!tierFastestTimes.TryGetValue(tier, out float time)) return "-";
			TimeSpan span = TimeSpan.FromSeconds(time);
			float seconds = span.Seconds + span.Milliseconds / 1000f;
			int tenth = (int)((seconds % 1f) * 10f);
			return "<size=110%><sprite name=clock></size> " + string.Format("{0:D2}:{1:00}<size=75%>.{2}</size>", span.Minutes, Math.Floor(seconds), tenth);
		}
	}
}
