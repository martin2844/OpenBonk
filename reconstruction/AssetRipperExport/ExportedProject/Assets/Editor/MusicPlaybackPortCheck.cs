using System;
using System.Reflection;
using Assets.Scripts.Audio.Music;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class MusicPlaybackPortCheck
{
    public static void Run()
    {
        var go = new GameObject("music playback check");
        var intro = AudioClip.Create("test intro", 4410, 1, 44100, false);
        var loop = AudioClip.Create("test loop", 4410, 1, 44100, false);
        var track = ScriptableObject.CreateInstance<MusicTrack>();
        try
        {
            var source = go.AddComponent<AudioSource>();
            var player = go.AddComponent<MusicController>();
            player.musicSource = source;
			var checkFilter = typeof(MusicController).GetMethod("UpdateDesiredLowpass", BindingFlags.Instance | BindingFlags.NonPublic);
			var checkTime = typeof(MusicController).GetField("nextCheckTime", BindingFlags.Instance | BindingFlags.NonPublic);
			var desiredFilter = typeof(MusicController).GetField("desiredLowpass", BindingFlags.Instance | BindingFlags.NonPublic);
			bool priorPause = MyTime.paused;
			try
			{
				MyTime.paused = true; checkTime.SetValue(player, -1f); checkFilter.Invoke(player, null);
				if ((float)desiredFilter.GetValue(player) != 600f) throw new Exception("Pause filter cutoff incorrect");
				MyTime.paused = false; checkTime.SetValue(player, -1f); checkFilter.Invoke(player, null);
				if ((float)desiredFilter.GetValue(player) != 22000f) throw new Exception("Normal filter cutoff incorrect");
			}
			finally { MyTime.paused = priorPause; }
            track.intro = intro;
            track.loop = loop;
			var other = ScriptableObject.CreateInstance<MusicTrack>();
			other.category = MusicCategory.Desert;
			track.category = MusicCategory.Forest;
			if (track.CompareTo(other) >= 0) throw new Exception("Track ordering must follow music category");
			UnityEngine.Object.DestroyImmediate(other);
            player.PlayMusicTrack(track);
            if (source.clip != intro || source.loop) throw new Exception("Intro was not selected first");
            var update = typeof(MusicController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            source.time = .06f;
            if (!source.isPlaying) throw new Exception("Audio source did not start for the intro timing check");
            update.Invoke(player, null);
            if (source.clip != intro) throw new Exception("Intro ended before its threshold");
            source.time = .085f;
            update.Invoke(player, null);
            if (source.clip != loop || !source.loop) throw new Exception("Loop did not start 20 ms before the intro ended");
            player.StopMusic(); player.PlayMusicTrack(track); source.Stop();
            update.Invoke(player, null);
            if (source.clip != loop || !source.loop) throw new Exception("Loop did not follow stopped intro");
            var menu = AssetDatabase.LoadAssetAtPath<MusicTrack>("Assets/MonoBehaviour/Menu.asset");
            if (menu == null || menu.loop == null) throw new Exception("Original menu track is missing");
            player.menuMusicTrack = menu;
            player.PlayMenuTrack();
            if (source.clip != menu.loop || !source.loop || Math.Abs(source.volume - .75f) > .001f) throw new Exception("Original menu loop was not prepared");
            player.SetMusicVolume(.4f);
            if (Math.Abs(source.volume - .4f) > .001f) throw new Exception("Music volume was not applied immediately");
            update.Invoke(player, null);
            if (Math.Abs(source.volume - .4f) > .001f) throw new Exception("Music volume was not retained during update");
            var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
            if (map == null || map.musicTracks == null || map.musicTracks.Length == 0) throw new Exception("Original Forest tracks missing");
            var selected = MusicUtility.GetMusicTrackToPlay(new RunConfig { mapData = map, musicTrackIndex = -1 });
            if (selected == null || Array.IndexOf(map.musicTracks, selected) < 0) throw new Exception("Forest did not select a map track");
            Debug.Log("MusicPlaybackPortCheck passed: menu=" + menu.trackName + ", Forest=" + selected.trackName);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(track); UnityEngine.Object.DestroyImmediate(intro); UnityEngine.Object.DestroyImmediate(loop); }
    }
}
