using System.Collections.Generic;
using Assets.Scripts.Game.Other;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Audio.Music
{
	public class MusicUtility
	{
		private static List<MusicTrack> tracks;

		private static List<MusicTrack> tracksOther;

		private static Dictionary<EMap, int> mapTrackRotation;

		private static MusicTrack themeTrackPlayedLastRound;

		public static MusicTrack GetMusicTrackToPlay(RunConfig runConfig)
		{
			if (runConfig == null) return null;
			if (MapController.IsFinalBossStage() && runConfig.mapData != null && runConfig.mapData.bossTrack != null)
				return runConfig.mapData.bossTrack;
			if (runConfig.musicTrackIndex >= 0)
			{
				var all = GetTracks();
				if (runConfig.musicTrackIndex < all.Count) return all[runConfig.musicTrackIndex];
			}
			CharacterData character = null;
			if (DataManager.Instance != null && DataManager.Instance.unsortedCharacterData != null)
				character = DataManager.Instance.unsortedCharacterData.Find(c => c != null && c.eCharacter == CharacterMenu.selectedCharacter);
			if (character != null && character.themeSong != null)
			{
				float roll = UnityEngine.Random.Range(0f, 1f);
				var progression = SaveManager.Instance != null && SaveManager.Instance.progression != null
					? SaveManager.Instance.progression.GetCharacterProgression(CharacterMenu.selectedCharacter) : null;
				if ((progression != null && progression.numRuns <= 0) || roll < .15f)
				{
					themeTrackPlayedLastRound = character.themeSong;
					return character.themeSong;
				}
			}
			themeTrackPlayedLastRound = null;
			if (UnityEngine.Random.Range(0f, 1f) < .15f)
			{
				var other = GetTracksOther();
				if (other.Count > 0) return other[UnityEngine.Random.Range(0, other.Count)];
			}
			if (runConfig.mapData == null || runConfig.mapData.musicTracks == null || runConfig.mapData.musicTracks.Length == 0) return null;
			var candidates = runConfig.mapData.musicTracks;
			if (mapTrackRotation == null) mapTrackRotation = new Dictionary<EMap, int>();
			int index;
			if (!mapTrackRotation.TryGetValue(runConfig.mapData.eMap, out index)) index = UnityEngine.Random.Range(0, candidates.Length);
			index %= candidates.Length;
			for (int attempts = 0; attempts < 101; attempts++)
			{
				if (candidates[index] != null && candidates[index].maxStageCompatibility >= MapController.index) break;
				index = (index + 1) % candidates.Length;
			}
			mapTrackRotation[runConfig.mapData.eMap] = (index + 1) % candidates.Length;
			return candidates[index];
		}

		public static List<MusicTrack> GetTracks()
		{
			if (DataManager.Instance == null || DataManager.Instance.unsortedMusic == null) return new List<MusicTrack>();
			var result = new List<MusicTrack>();
			foreach (var track in DataManager.Instance.unsortedMusic)
				if (track != null && track.isEnabled && track.isInJukebox) result.Add(track);
			result.Sort();
			return result;
		}

		public static List<MusicTrack> GetTracksOther()
		{
			if (DataManager.Instance == null || DataManager.Instance.unsortedMusic == null) return new List<MusicTrack>();
			var result = new List<MusicTrack>();
			foreach (var track in DataManager.Instance.unsortedMusic)
				if (track != null && track.isEnabled && track.isInJukebox && track.isInRandomPool && track.category == MusicCategory.Other) result.Add(track);
			result.Sort();
			return result;
		}
	}
}
