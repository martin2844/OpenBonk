using System;
using UnityEngine;

namespace Assets.Scripts.Audio.Music
{
	[CreateAssetMenu(menuName = "Me/MusicTrack", order = 1)]
	public class MusicTrack : ScriptableObject, IComparable<MusicTrack>
	{
		public bool isEnabled;

		public bool isInJukebox;

		public bool isInRandomPool;

		public int maxStageCompatibility;

		public MusicCategory category;

		public string trackName;

		public AudioClip intro;

		public AudioClip loop;

		public void LoadToMemory()
		{
			if (intro != null) intro.LoadAudioData();
			if (loop != null) loop.LoadAudioData();
		}

		public void UnloadFromMemory()
		{
			if (intro != null) intro.UnloadAudioData();
			if (loop != null && loop != intro) loop.UnloadAudioData();
		}

		public bool IsLoadedInMemory()
		{
			return (intro == null || intro.loadState == AudioDataLoadState.Loaded) && (loop == null || loop.loadState == AudioDataLoadState.Loaded);
		}

		public int CompareTo(MusicTrack other)
		{
			return other == null ? 1 : category.CompareTo(other.category);
		}
	}
}
